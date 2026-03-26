namespace AtroxCondoSuite.Runtime.Api.EntryPoints.Sqs.Handlers
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Messaging;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Services;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Request;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;
    using Microsoft.Extensions.Logging;

    public sealed class AtroxCondoSuiteQueueProcessor(
        IAtroxCondoSuiteApiServices service,
        ILogger<AtroxCondoSuiteQueueProcessor> logger) : IAtroxCondoSuiteQueueProcessor
    {
        private readonly IAtroxCondoSuiteApiServices _service = service ?? throw new ArgumentNullException(nameof(service));
        private readonly ILogger<AtroxCondoSuiteQueueProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<ServiceResult> ProcessAsync(ApplicationDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing queued request for procedure {ProcedureName}", request?.Data?.procedureName);
            var result = await _service.ExecuteAsync(request, cancellationToken);
            _logger.LogInformation("Queued request completed with {ErrorCount} errors", result.Error?.Count ?? 0);
            return result;
        }
    }
}

