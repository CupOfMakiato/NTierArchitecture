namespace NTierArchitecture.API.Middlewares
{
    public class RateLimitMiddleware : IMiddleware
    {
        private const int MaxRequests = 40; 
        private static int _requestCounter;
        private static DateTime _startTime = DateTime.UtcNow;
        private static readonly object _lock = new();

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            bool limitExceeded = false;

            lock (_lock)
            {
                var currentTime = DateTime.UtcNow;
                var elapsedSeconds = (currentTime - _startTime).TotalSeconds;

                // reset counter each second
                if (elapsedSeconds >= 1)
                {
                    _requestCounter = 0;
                    _startTime = currentTime;
                }

                _requestCounter++;

                if (_requestCounter > MaxRequests)
                {
                    limitExceeded = true;
                }
            }

            if (limitExceeded)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }

            await next(context);
        }
    }
}

