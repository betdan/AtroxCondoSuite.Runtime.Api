namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.Security
{
    public interface IStoredProcedureAuthorizationService
    {
        Task<bool> IsExecutionAllowedAsync(string databaseName, string procedureName, string tenantId, CancellationToken cancellationToken = default);
    }
}

