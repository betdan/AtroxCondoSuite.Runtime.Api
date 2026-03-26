namespace AtroxCondoSuite.Runtime.Api.EntryPoints.Http.Middleware
{
    using Serilog.Context;

    public sealed class RequestCorrelationMiddleware(RequestDelegate next)
    {
        public const string CorrelationHeader = "x-correlation-id";
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

        public async Task Invoke(HttpContext context)
        {
            var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
            }

            context.Response.Headers[CorrelationHeader] = correlationId;
            context.Items[CorrelationHeader] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("RequestPath", context.Request.Path.ToString()))
            {
                await _next(context);
            }
        }
    }
}

