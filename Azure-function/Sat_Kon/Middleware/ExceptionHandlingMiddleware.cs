using Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
namespace Sat_Kon.Middleware
{
    /// <summary>
    /// Global exception handling middleware to capture unhandled exceptions,
    /// log them using structured logging, and return details as a JSON response.
    /// Note: Exposing full exception details is temporary for debugging purposes in production.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }



        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            object errorResponse;

            // 1. FluentValidation errors
            if (exception is ValidationException validationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                errorResponse = new
                {
                    success = false,
                    errors = validationException.Errors.Select(x => new
                    {
                        field = x.PropertyName,
                        message = x.ErrorMessage
                    })
                };
            }

            // 2. Domain exceptions
            else if (exception is Domain.Exceptions.NotFoundException notFoundEx)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                errorResponse = new { success = false, message = notFoundEx.Message };
            }
            else if (exception is Domain.Exceptions.BadRequestException domainBadRequestEx)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                errorResponse = new { success = false, message = domainBadRequestEx.Message };
            }
            else if (exception is Domain.Exceptions.GoneException goneEx)
            {
                context.Response.StatusCode = StatusCodes.Status410Gone;
                errorResponse = new { success = false, message = goneEx.Message };
            }
            else if (exception is Domain.Exceptions.ForbiddenException forbiddenEx)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                errorResponse = new { success = false, message = forbiddenEx.Message };
            }
            // 3. Application level custom exceptions
            else if (exception is BadRequestException badRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                errorResponse = new { success = false, message = badRequest.Message };
            }

            // 3. fallback
            else
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                errorResponse = new
                {
                    success = false,
                    message = "An unexpected error occurred."
                };
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
        }
    }
}
