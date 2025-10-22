using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MediaVoyager.Middleware
{
    public class HttpRequestExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpRequestExceptionMiddleware> _logger;

        public HttpRequestExceptionMiddleware(RequestDelegate next, ILogger<HttpRequestExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("429 Too Many Requests: {Message}", ex.Message);

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.ContentType = "application/json";

                    var payload = new { error = "Too Many Requests", message = ex.Message };
                    var json = JsonSerializer.Serialize(payload);
                    await context.Response.WriteAsync(json);
                }
            }
        }
    }

    public static class HttpRequestExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequestExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<HttpRequestExceptionMiddleware>();
        }
    }
}
