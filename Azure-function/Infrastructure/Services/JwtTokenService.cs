using Domain.Dto;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace Infrastructure.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtTokenService> _logger;

        public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public JwtTokenResult GenerateToken(User user)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            var keyString = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(keyString))
            {
                _logger.LogError("JWT Generation failed: JWT Key is missing in configuration.");
                throw new Exception("JWT Key is missing in configuration");
            }

            if (!int.TryParse(_configuration["Jwt:ExpiresMinutes"], out var expiresMinutes))
                expiresMinutes = 60;

            var jti = Guid.NewGuid().ToString();

            // Normalize role using enum to prevent invalid role injection
            var roleValue = user.Role;
            if (!Enum.TryParse(typeof(Domain.Enums.UserRole), roleValue, true, out var parsedRole))
            {
                _logger.LogWarning("User {UserId} has invalid role value: {Role}. Defaulting to 'User'.", user.Id, roleValue);
                roleValue = Domain.Enums.UserRole.User.ToString();
            }

            var claims = new List<Claim>
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email ?? string.Empty),
                new Claim("role", roleValue),
                new Claim(JwtRegisteredClaimNames.Jti, jti)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryTime = DateTime.UtcNow.AddMinutes(expiresMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiryTime,
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshToken = GenerateRefreshToken();

            _logger.LogInformation(
                "Successfully generated JWT token for User: {UserId}, Email: {Email}, Role: {Role}, JTI: {Jti}, Expiry: {Expiry}",
                user.Id, user.Email, roleValue, jti, expiryTime
            );

            return new JwtTokenResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiryTime = expiryTime
            };
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = validateLifetime,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, parameters, out var securityToken);
                if (securityToken is JwtSecurityToken jwt &&
                    jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return principal;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);
                return null;
            }
        }
    }
}
