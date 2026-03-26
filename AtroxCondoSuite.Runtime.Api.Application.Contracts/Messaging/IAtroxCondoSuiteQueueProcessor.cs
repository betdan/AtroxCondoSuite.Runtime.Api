namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.Messaging
{
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Request;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;

    public interface IAtroxCondoSuiteQueueProcessor
    {
        Task<ServiceResult> ProcessAsync(ApplicationDto request, CancellationToken cancellationToken = default);
    }
}

