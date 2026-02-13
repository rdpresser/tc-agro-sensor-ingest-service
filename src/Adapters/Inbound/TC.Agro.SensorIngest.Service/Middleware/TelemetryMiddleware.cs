using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

namespace TC.Agro.SensorIngest.Service.Middleware
{
    public class TelemetryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TelemetryMiddleware> _logger;
        private readonly SensorIngestMetrics _ingestMetrics;
        private readonly SystemMetrics _systemMetrics;

        public TelemetryMiddleware(
            RequestDelegate next,
            ILogger<TelemetryMiddleware> logger,
            SensorIngestMetrics ingestMetrics,
            SystemMetrics systemMetrics)
        {
            _next = next;
            _logger = logger;
            _ingestMetrics = ingestMetrics;
            _systemMetrics = systemMetrics;
        }

        public async Task InvokeAsync(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
        {
            var stopwatch = Stopwatch.StartNew();
            var path = context.Request.Path.Value ?? "/";

            if (ShouldSkipTelemetry(path))
            {
                await _next(context);
                return;
            }

            var userId = ExtractUserId(context);
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            var userRoles = ExtractUserRoles(context);

            var correlationId = correlationIdGenerator.CorrelationId ?? context.TraceIdentifier ?? "unknown";

            using (LogContext.PushProperty("correlation_id", correlationId))
            using (LogContext.PushProperty("user.id", userId))
            using (LogContext.PushProperty("user.authenticated", isAuthenticated))
            {
                using var activity = ActivitySourceFactory.Handlers.StartActivity($"http_request_{context.Request.Method}");

                if (activity != null)
                {
                    activity.SetTag("http.method", context.Request.Method);
                    activity.SetTag("http.path", path);
                    activity.SetTag("http.target", context.Request.Path + context.Request.QueryString);
                    activity.SetTag("user.id", userId);
                    activity.SetTag("user.authenticated", isAuthenticated);
                    activity.SetTag("correlation_id", correlationId);

                    if (isAuthenticated && !string.IsNullOrWhiteSpace(userRoles))
                    {
                        activity.SetTag("user.roles", userRoles);
                    }
                }

                try
                {
                    if (isAuthenticated)
                    {
                        _ingestMetrics.RecordIngestAction($"{context.Request.Method}", userId, path);
                    }

                    await _next(context);

                    stopwatch.Stop();
                    var durationSeconds = stopwatch.Elapsed.TotalSeconds;

                    _systemMetrics.RecordHttpRequest(context.Request.Method, path, context.Response.StatusCode, durationSeconds);

                    if (activity != null)
                    {
                        activity.SetTag("http.status_code", context.Response.StatusCode);
                        activity.SetTag("http.duration_ms", stopwatch.ElapsedMilliseconds);

                        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                        {
                            activity.SetStatus(ActivityStatusCode.Ok);
                            LogSuccessResponse(context, path, durationSeconds, userId, correlationId);
                        }
                        else if (context.Response.StatusCode >= 300 && context.Response.StatusCode < 400)
                        {
                            activity.SetStatus(ActivityStatusCode.Ok);
                            LogRedirectResponse(context, path, durationSeconds, userId, correlationId);
                        }
                        else if (context.Response.StatusCode >= 400 && context.Response.StatusCode < 500)
                        {
                            activity.SetStatus(ActivityStatusCode.Error, "Client Error");
                            LogClientErrorResponse(context, path, durationSeconds, userId, correlationId);
                        }
                        else if (context.Response.StatusCode >= 500)
                        {
                            activity.SetStatus(ActivityStatusCode.Error, "Server Error");
                            LogServerErrorResponse(context, path, durationSeconds, userId, correlationId);
                        }
                    }
                }
#pragma warning disable S2139
                catch (Exception ex)
#pragma warning restore S2139
                {
                    stopwatch.Stop();
                    var durationSeconds = stopwatch.Elapsed.TotalSeconds;

                    _systemMetrics.RecordHttpRequest(context.Request.Method, path, context.Response.StatusCode, durationSeconds);

                    if (activity != null)
                    {
                        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                        activity.SetTag("error.type", ex.GetType().Name);
                        activity.SetTag("error.message", ex.Message);
                        activity.SetTag("http.duration_ms", stopwatch.ElapsedMilliseconds);
                    }

                    _logger.LogError(ex,
                        "Request {Method} {Path} failed after {DurationMs}ms for user {UserId} with correlation {CorrelationId}",
                        context.Request.Method, path, stopwatch.ElapsedMilliseconds, userId, correlationId);

                    throw;
                }
            }
        }

        private static bool ShouldSkipTelemetry(string path)
        {
            return path.Contains("/health", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("/metrics", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("/prometheus", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("/swagger", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractUserId(HttpContext context)
        {
            var sub = context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(sub))
                return sub;

            var nameId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(nameId))
                return nameId;

            var name = context.User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            return TelemetryConstants.AnonymousUser;
        }

        private static string ExtractUserRoles(HttpContext context)
        {
            var roles = context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return roles?.Count > 0 ? string.Join(",", roles) : "";
        }

        private void LogSuccessResponse(HttpContext context, string path, double durationSeconds, string userId, string correlationId)
        {
            _logger.LogInformation(
                "Request {Method} {Path} completed successfully in {DurationMs}ms with status {StatusCode} for user {UserId} with correlation {CorrelationId}",
                context.Request.Method, path, (long)(durationSeconds * 1000), context.Response.StatusCode, userId, correlationId);
        }

        private void LogRedirectResponse(HttpContext context, string path, double durationSeconds, string userId, string correlationId)
        {
            _logger.LogInformation(
                "Request {Method} {Path} redirected in {DurationMs}ms with status {StatusCode} for user {UserId} with correlation {CorrelationId}",
                context.Request.Method, path, (long)(durationSeconds * 1000), context.Response.StatusCode, userId, correlationId);
        }

        private void LogClientErrorResponse(HttpContext context, string path, double durationSeconds, string userId, string correlationId)
        {
            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode == 401 || statusCode == 403 ? "Security-related" : "Client";

            _logger.LogWarning(
                "{LogLevel} error: Request {Method} {Path} completed with status {StatusCode} in {DurationMs}ms for user {UserId} with correlation {CorrelationId}",
                logLevel, context.Request.Method, path, statusCode, (long)(durationSeconds * 1000), userId, correlationId);
        }

        private void LogServerErrorResponse(HttpContext context, string path, double durationSeconds, string userId, string correlationId)
        {
            _logger.LogError(
                "Request {Method} {Path} server error in {DurationMs}ms with status {StatusCode} for user {UserId} with correlation {CorrelationId}",
                context.Request.Method, path, (long)(durationSeconds * 1000), context.Response.StatusCode, userId, correlationId);
        }
    }
}
