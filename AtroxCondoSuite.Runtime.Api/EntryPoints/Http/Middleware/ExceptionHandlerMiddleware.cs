namespace AtroxCondoSuite.Runtime.Api.EntryPoints.Http.Middleware
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Common;
    using Microsoft.Extensions.Logging;
    using System.Net;
    using System.Text.Json;

    public sealed class ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
        private readonly ILogger<ExceptionHandlerMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing request {Path}", context.Request.Path);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                var header = ResolveHeader(context);

                var response = new ServiceResponse<object>
                {
                    Success = false,
                    Header = header,
                    Code = "UNHANDLED_ERROR",
                    Message = "An unexpected error occurred.",
                    Errors = new Dictionary<string, string[]>
                    {
                        ["exceptions"] = [ex.Message]
                    },
                    Data = null
                };

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
            }
        }

        private static StandardHeader ResolveHeader(HttpContext context)
        {
            if (context.Items.TryGetValue(RequestHeaderValidationMiddleware.StandardHeaderKey, out var value)
                && value is StandardHeader header)
            {
                header.Timestamp = DateTimeOffset.UtcNow;
                header.TraceId = context.TraceIdentifier;
                return header;
            }

            return new StandardHeader
            {
                CorrelationId = context.Response.Headers[RequestCorrelationMiddleware.CorrelationHeader].FirstOrDefault()
                                ?? Guid.NewGuid().ToString("N"),
                TransactionId = Guid.NewGuid().ToString("N"),
                SessionId = Guid.NewGuid().ToString("N"),
                Channel = "HTTP",
                UserId = "anonymous",
                ClientIp = context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0",
                TraceId = context.TraceIdentifier,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}

