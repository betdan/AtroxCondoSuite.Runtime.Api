namespace AtroxCondoSuite.Runtime.Api.EntryPoints.Http.Middleware
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Common;
    using Microsoft.Extensions.Logging;
    using System.Net;
    using System.Text.Json;

    public sealed class RequestHeaderValidationMiddleware(RequestDelegate next, ILogger<RequestHeaderValidationMiddleware> logger)
    {
        public const string StandardHeaderKey = "standard-header";

        private const string TransactionHeader = "x-transaction-id";
        private const string SessionHeader = "x-session-id";
        private const string ChannelHeader = "x-channel";
        private const string UserHeader = "x-user-id";
        private const string ClientIpHeader = "x-client-ip";

        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
        private readonly ILogger<RequestHeaderValidationMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public async Task Invoke(HttpContext context)
        {
            var transactionId = ResolveGuidOrNew(context, TransactionHeader);
            var errors = new Dictionary<string, string[]>();

            var sessionId = ResolveRequiredGuid(context, SessionHeader, errors);
            var channel = ResolveRequiredHeader(context, ChannelHeader, errors);
            var userId = ResolveRequiredHeader(context, UserHeader, errors);
            var clientIp = ResolveRequiredHeader(context, ClientIpHeader, errors);

            var correlationId = context.Response.Headers[RequestCorrelationMiddleware.CorrelationHeader].FirstOrDefault()
                                ?? context.Request.Headers[RequestCorrelationMiddleware.CorrelationHeader].FirstOrDefault()
                                ?? Guid.NewGuid().ToString("N");

            if (errors.Count > 0)
            {
                await WriteBadRequestAsync(context, correlationId, transactionId, sessionId, errors);
                return;
            }

            var header = new StandardHeader
            {
                CorrelationId = correlationId,
                TransactionId = transactionId,
                SessionId = sessionId,
                Channel = channel,
                UserId = userId,
                ClientIp = clientIp,
                TraceId = context.TraceIdentifier,
                Timestamp = DateTimeOffset.UtcNow
            };

            context.Items[StandardHeaderKey] = header;

            context.Response.Headers[RequestCorrelationMiddleware.CorrelationHeader] = correlationId;
            context.Response.Headers[TransactionHeader] = transactionId;
            context.Response.Headers[SessionHeader] = sessionId;

            await _next(context);
        }

        private static string ResolveGuidOrNew(HttpContext context, string headerName)
        {
            var raw = context.Request.Headers[headerName].FirstOrDefault();
            if (Guid.TryParse(raw, out var parsed))
            {
                return parsed.ToString("N");
            }

            return Guid.NewGuid().ToString("N");
        }

        private static string ResolveRequiredHeader(HttpContext context, string headerName, Dictionary<string, string[]> errors)
        {
            var raw = context.Request.Headers[headerName].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(raw))
            {
                errors[headerName] = ["Header is required."];
                return string.Empty;
            }
            return raw.Trim();
        }

        private static string ResolveRequiredGuid(HttpContext context, string headerName, Dictionary<string, string[]> errors)
        {
            var raw = context.Request.Headers[headerName].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(raw))
            {
                errors[headerName] = ["Header is required."];
                return string.Empty;
            }

            if (!Guid.TryParse(raw, out var parsed))
            {
                errors[headerName] = ["Header must be a valid GUID."];
                return string.Empty;
            }

            return parsed.ToString("N");
        }

        private static async Task WriteBadRequestAsync(
            HttpContext context,
            string correlationId,
            string transactionId,
            string sessionId,
            IReadOnlyDictionary<string, string[]> errors)
        {
            var response = new ServiceResponse<object>
            {
                Success = false,
                Header = new StandardHeader
                {
                    CorrelationId = correlationId,
                    TransactionId = transactionId,
                    SessionId = sessionId,
                    TraceId = context.TraceIdentifier,
                    Timestamp = DateTimeOffset.UtcNow
                },
                Code = "INVALID_HEADER",
                Message = "Missing or invalid required headers.",
                Errors = new Dictionary<string, string[]>(errors)
            };

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            context.Response.Headers[RequestCorrelationMiddleware.CorrelationHeader] = correlationId;
            context.Response.Headers[TransactionHeader] = transactionId;
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                context.Response.Headers[SessionHeader] = sessionId;
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
        }
    }
}

