namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.Services
{
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Request;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;

    public interface IAtroxCondoSuiteApiServices
    {
        Task<ServiceResult> ExecuteAsync(ApplicationDto request, CancellationToken cancellationToken = default);
    }
}





