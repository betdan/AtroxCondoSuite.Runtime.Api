namespace AtroxCondoSuite.Runtime.Api.EntryPoints.Http.Middleware
{
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Metrics;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Common;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class ResponseMetricMiddleware(RequestDelegate request)
    {
        private readonly RequestDelegate _request = request ?? throw new ArgumentNullException(nameof(request));

        public async Task Invoke(HttpContext httpContext, MetricCollector collector, IOptions<ObservabilityOptions> options, ILogger<ResponseMetricMiddleware> logger)
        {
            var observability = options?.Value ?? new ObservabilityOptions();
            var path = httpContext.Request.Path.Value;

            if (path == "/metrics")
            {
                await _request.Invoke(httpContext);
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _request.Invoke(httpContext);
            }
            finally
            {
                stopwatch.Stop();
                var header = httpContext.Items.TryGetValue(RequestHeaderValidationMiddleware.StandardHeaderKey, out var headerObj)
                    ? headerObj as StandardHeader
                    : null;

                if (observability.EnableRequestMetrics)
                {
                    collector.RegisterRequest();
                    collector.RegisterResponseTime(httpContext.Response.StatusCode, httpContext.Request.Method, stopwatch.Elapsed);
                }

                using var scope = logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = header?.CorrelationId,
                    ["TransactionId"] = header?.TransactionId,
                    ["SessionId"] = header?.SessionId,
                    ["Channel"] = header?.Channel
                });

                logger.LogInformation(
                    "Request metrics {Method} {Path} => {StatusCode} in {ElapsedMs}ms",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    httpContext.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}

