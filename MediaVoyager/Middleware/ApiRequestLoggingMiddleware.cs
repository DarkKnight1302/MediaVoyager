using MediaVoyager.Repositories;

namespace MediaVoyager.Middleware
{
    public class ApiRequestLoggingMiddleware
    {
        private readonly RequestDelegate next;

        public ApiRequestLoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context, IApiRequestLogRepository repository, ILogger<ApiRequestLoggingMiddleware> logger)
        {
            var start = DateTimeOffset.UtcNow;
            try
            {
                await next(context);
            }
            finally
            {
                try
                {
                    // Only log API endpoints to keep noise down
                    var path = context.Request.Path.Value ?? string.Empty;
                    if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                    {
                        // Normalize to first two path segments: /api/{controller}
                        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        var api = segments.Length >= 2 ? $"/api/{segments[1]}" : "/api";

                        var userId = context.Request.Headers["x-uid"].FirstOrDefault();
                        int statusCode = context.Response?.StatusCode ?? 0;

                        await repository.LogAsync(api, context.Request.Method, statusCode, start, userId, path);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to record API request log");
                }
            }
        }
    }

    public static class ApiRequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApiRequestLoggingMiddleware>();
        }
    }
}
