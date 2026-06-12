using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NTierArchitecture.API.Middlewares
{
    public class PerformanceMiddleware : IMiddleware
    {
        private readonly Stopwatch _stopwatch;
        private readonly ILogger<PerformanceMiddleware> _logger; // Use ILogger

        public PerformanceMiddleware(ILogger<PerformanceMiddleware> logger)
        {
            _stopwatch = new Stopwatch();
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var path = context.Request.Path;
            var method = context.Request.Method;

            if (!path.StartsWithSegments("/api"))
            {
                await next(context);
                return;
            }

            _stopwatch.Restart();
            _logger.LogInformation($"[Performance] {method} {path} - Started");

            await next(context);

            _stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            _logger.LogInformation($"[Performance] {method} {path} - Completed with status {statusCode} in {_stopwatch.ElapsedMilliseconds} ms");
        }

    }
}
