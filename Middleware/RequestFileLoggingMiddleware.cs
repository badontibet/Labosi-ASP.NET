using System.Diagnostics;
using System.Security.Claims;
using NasIndexer.Services;

namespace NasIndexer.Middleware
{
    public class RequestFileLoggingMiddleware
    {
        private static readonly string[] SensitiveQueryFragments =
        {
            "password",
            "clientid",
            "client_id",
            "clientsecret",
            "client_secret",
            "secret",
            "token",
            "code",
            "authorization",
            "cookie"
        };

        private readonly RequestDelegate next;
        private readonly AppFileLogger logger;

        public RequestFileLoggingMiddleware(RequestDelegate next, AppFileLogger logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await next(context);
                stopwatch.Stop();

                await logger.LogAsync(
                    "INFO",
                    BuildRequestMessage(context, stopwatch.ElapsedMilliseconds));
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                await logger.LogAsync(
                    "ERROR",
                    BuildExceptionMessage(context, exception));

                throw;
            }
        }

        private static string BuildRequestMessage(HttpContext context, long elapsedMilliseconds)
        {
            return string.Join(
                ' ',
                $"method={AppFileLogger.Sanitize(context.Request.Method)}",
                $"path={AppFileLogger.Sanitize(context.Request.Path.Value)}",
                $"query={GetSafeQueryString(context)}",
                $"status={context.Response.StatusCode}",
                $"elapsedMs={elapsedMilliseconds}",
                $"user={GetUserName(context)}");
        }

        private static string BuildExceptionMessage(HttpContext context, Exception exception)
        {
            return string.Join(
                ' ',
                $"method={AppFileLogger.Sanitize(context.Request.Method)}",
                $"path={AppFileLogger.Sanitize(context.Request.Path.Value)}",
                $"user={GetUserName(context)}",
                $"exceptionType={AppFileLogger.Sanitize(exception.GetType().FullName)}",
                $"exceptionMessage={AppFileLogger.Sanitize(exception.Message)}",
                $"stackTrace={AppFileLogger.Sanitize(exception.StackTrace)}");
        }

        private static string GetSafeQueryString(HttpContext context)
        {
            if (!context.Request.QueryString.HasValue)
            {
                return "-";
            }

            foreach (var key in context.Request.Query.Keys)
            {
                var normalizedKey = key.Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
                if (SensitiveQueryFragments.Any(fragment => normalizedKey.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                {
                    return "[redacted]";
                }
            }

            return AppFileLogger.Sanitize(context.Request.QueryString.Value);
        }

        private static string GetUserName(HttpContext context)
        {
            var userName = context.User.FindFirstValue(ClaimTypes.Email)
                ?? context.User.Identity?.Name;

            return string.IsNullOrWhiteSpace(userName)
                ? "anonymous"
                : AppFileLogger.Sanitize(userName);
        }
    }
}
