namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.Messaging
{
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;

    public interface IAtroxCondoSuiteSqsMessageHandler
    {
        Task<ServiceResult> HandleAsync(string messageBody, CancellationToken cancellationToken = default);
    }
}

