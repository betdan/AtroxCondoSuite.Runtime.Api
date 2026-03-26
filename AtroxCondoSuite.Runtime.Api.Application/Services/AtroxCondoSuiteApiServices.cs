namespace AtroxCondoSuite.Runtime.Api.Application.Services
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Services;
    using AtroxCondoSuite.Runtime.Api.Application.Errors;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Security;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Request;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;
    using AtroxCondoSuite.Runtime.Api.DataAccess.Infrastructure.SqlServer.Executions;
    using Microsoft.Extensions.Logging;

    public class AtroxCondoSuiteApiServices(
        ILogger<AtroxCondoSuiteApiServices> logger,
        AtroxCondoSuiteApiExecute db,
        IStoredProcedureAuthorizationService authorizationService) : IAtroxCondoSuiteApiServices
    {
        private readonly ILogger<AtroxCondoSuiteApiServices> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly AtroxCondoSuiteApiExecute _db = db ?? throw new ArgumentNullException(nameof(db));
        private readonly IStoredProcedureAuthorizationService _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));

        public async Task<ServiceResult> ExecuteAsync(ApplicationDto request, CancellationToken cancellationToken = default)
        {
            if (request?.Data == null)
            {
                return ServiceResultFactory.Failure(
                    ApiErrorCatalog.InvalidRequest.Code,
                    ApiErrorCatalog.InvalidRequest.Message);
            }

            var response = new ServiceResult();
            var isAllowed = await _authorizationService.IsExecutionAllowedAsync(
                request.Data.databaseName,
                request.Data.procedureName,
                request.Data.tenantId,
                cancellationToken);

            if (!isAllowed)
            {
                response = ServiceResultFactory.Failure(
                    ApiErrorCatalog.StoredProcedureNotAuthorized.Code,
                    ApiErrorCatalog.StoredProcedureNotAuthorized.Message);

                _logger.LogWarning("Stored procedure {ProcedureName} denied by authorization policy", request.Data.procedureName);
                return response;
            }

            var databaseResponse = await _db.ExecuteAsync(request, cancellationToken);
            response.Data = databaseResponse;

            response.Error = (databaseResponse.raisError ?? [])
                .Select(item => ServiceResultFactory.CreateError(item.code.ToString(), item.message))
                .ToList();

            _logger.LogInformation(
                "Stored procedure execution completed. ReturnValue={ReturnValue}, ErrorCount={ErrorCount}",
                databaseResponse.returns,
                response.Error.Count);

            return response;
        }
    }
}





