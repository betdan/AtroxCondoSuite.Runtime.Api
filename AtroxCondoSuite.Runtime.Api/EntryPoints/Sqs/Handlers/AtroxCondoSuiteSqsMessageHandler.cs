namespace AtroxCondoSuite.Runtime.Api.EntryPoints.Sqs.Handlers
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Messaging;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Common;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Request;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;
    using Microsoft.Extensions.Logging;
    using System.Text.Json;

    public sealed class AtroxCondoSuiteSqsMessageHandler(
        IAtroxCondoSuiteQueueProcessor queueProcessor,
        IAtroxCondoSuiteNotificationPublisher notificationPublisher,
        ILogger<AtroxCondoSuiteSqsMessageHandler> logger) : IAtroxCondoSuiteSqsMessageHandler
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IAtroxCondoSuiteQueueProcessor _queueProcessor = queueProcessor ?? throw new ArgumentNullException(nameof(queueProcessor));
        private readonly IAtroxCondoSuiteNotificationPublisher _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        private readonly ILogger<AtroxCondoSuiteSqsMessageHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<ServiceResult> HandleAsync(string messageBody, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageBody))
            {
                throw new ArgumentException("The SQS message body cannot be empty.", nameof(messageBody));
            }

            var serviceRequest = JsonSerializer.Deserialize<ServiceRequest<ApplicationDto>>(messageBody, SerializerOptions);
            var requestEnvelope = JsonSerializer.Deserialize<ApiRequestEnvelope<ApplicationDto>>(messageBody, SerializerOptions);
            var request = serviceRequest?.Body
                          ?? requestEnvelope?.Body
                          ?? JsonSerializer.Deserialize<ApplicationDto>(messageBody, SerializerOptions);

            if (request?.Data == null)
            {
                throw new InvalidOperationException("The SQS message body does not contain a valid application request.");
            }

            EnsureAuditHeaders(request, "SQS");

            _logger.LogInformation("Processing SQS message for procedure {ProcedureName}", request.Data.procedureName);

            var result = await _queueProcessor.ProcessAsync(request, cancellationToken);
            await _notificationPublisher.PublishAsync(result, cancellationToken);

            _logger.LogInformation("Finished SQS message for procedure {ProcedureName} with {ErrorCount} errors", request.Data.procedureName, result.Error?.Count ?? 0);

            return result;
        }

        private static void EnsureAuditHeaders(ApplicationDto request, string channel)
        {
            request.Data.inputParameters ??= [];

            var clientIp = "0.0.0.0";
            var userId = "sqs";
            var sessionId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var resolvedChannel = channel;

            if (!request.Data.inputParameters.Any(p => p?.paramName?.Equals("@i_client_ip", StringComparison.OrdinalIgnoreCase) == true))
                request.Data.inputParameters.Insert(0, new InputParamsRequest { paramName = "@i_client_ip", value = clientIp });

            if (!request.Data.inputParameters.Any(p => p?.paramName?.Equals("@i_user_id", StringComparison.OrdinalIgnoreCase) == true))
                request.Data.inputParameters.Insert(0, new InputParamsRequest { paramName = "@i_user_id", value = userId });

            if (!request.Data.inputParameters.Any(p => p?.paramName?.Equals("@i_channel", StringComparison.OrdinalIgnoreCase) == true))
                request.Data.inputParameters.Insert(0, new InputParamsRequest { paramName = "@i_channel", value = resolvedChannel });

            if (!request.Data.inputParameters.Any(p => p?.paramName?.Equals("@i_session_id", StringComparison.OrdinalIgnoreCase) == true))
                request.Data.inputParameters.Insert(0, new InputParamsRequest { paramName = "@i_session_id", value = sessionId });

            if (!request.Data.inputParameters.Any(p => p?.paramName?.Equals("@i_transaction_id", StringComparison.OrdinalIgnoreCase) == true))
                request.Data.inputParameters.Insert(0, new InputParamsRequest { paramName = "@i_transaction_id", value = transactionId });
        }
    }
}

