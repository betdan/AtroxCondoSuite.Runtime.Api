namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.Messaging
{
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;

    public interface IAtroxCondoSuiteNotificationPublisher
    {
        Task PublishAsync(ServiceResult payload, CancellationToken cancellationToken = default);
    }
}

