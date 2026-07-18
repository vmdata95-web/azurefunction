using System;

namespace Domain.Dto
{
    public class JwtTokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
    }
}
