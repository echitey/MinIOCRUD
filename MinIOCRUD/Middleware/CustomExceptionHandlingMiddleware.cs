using MinIOCRUD.Dtos;
using System.Net;
using System.Text.Json;

namespace MinIOCRUD.Middleware
{
    public class CustomExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomExceptionHandlingMiddleware> _logger;

        public CustomExceptionHandlingMiddleware(RequestDelegate next, ILogger<CustomExceptionHandlingMiddleware> logger)
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
                _logger.LogError(ex, "Unhandled exception occurred");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = ApiResponse<object>.FromException(ex, context.Response.StatusCode);
                var json = JsonSerializer.Serialize(response);

                await context.Response.WriteAsync(json);
            }
        }
    }
}
