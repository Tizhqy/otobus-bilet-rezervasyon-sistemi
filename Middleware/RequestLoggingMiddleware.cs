using System.Diagnostics;
using System.Security.Claims;

namespace OtobusBiletRezervasyon.Middleware
{
    public sealed class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = Stopwatch.GetTimestamp();
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            context.Response.Headers["X-Correlation-ID"] = traceId;

            var requestPath = context.Request.Path.HasValue
                ? context.Request.Path.Value!
                : "/";
            var requestMethod = context.Request.Method;
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var clientIp = GetClientIp(context);

            try
            {
                await _next(context);

                var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
                var statusCode = context.Response.StatusCode;
                var level = statusCode switch
                {
                    >= 500 => LogLevel.Error,
                    >= 400 => LogLevel.Warning,
                    _ => LogLevel.Information
                };

                _logger.Log(
                    level,
                    "HTTP {Method} {Path} => {StatusCode} in {ElapsedMs:0.000} ms (TraceId: {TraceId}, UserId: {UserId}, Ip: {Ip})",
                    requestMethod,
                    requestPath,
                    statusCode,
                    elapsedMs,
                    traceId,
                    userId,
                    clientIp);
            }
            catch (Exception ex)
            {
                var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
                _logger.LogError(
                    ex,
                    "HTTP {Method} {Path} failed in {ElapsedMs:0.000} ms (TraceId: {TraceId}, UserId: {UserId}, Ip: {Ip})",
                    requestMethod,
                    requestPath,
                    elapsedMs,
                    traceId,
                    userId,
                    clientIp);
                throw;
            }
        }

        private static string GetClientIp(HttpContext context)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var firstIp = forwarded
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(firstIp))
                {
                    return firstIp;
                }
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
