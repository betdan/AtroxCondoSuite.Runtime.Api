namespace AtroxCondoSuite.Runtime.Api.Infrastructure.Security
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Caching;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Security;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration;
    using AtroxCondoSuite.Runtime.Api.DataAccess.Contracts.Connections;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System.Data;

    public sealed class StoredProcedureAuthorizationService(
        ISqlServerDatabase sqlServerDatabase,
        IExternalCacheService externalCacheService,
        IOptions<StoredProcedureSecurityOptions> options,
        ILogger<StoredProcedureAuthorizationService> logger) : IStoredProcedureAuthorizationService
    {
        private readonly ISqlServerDatabase _sqlServerDatabase = sqlServerDatabase ?? throw new ArgumentNullException(nameof(sqlServerDatabase));
        private readonly IExternalCacheService _externalCacheService = externalCacheService ?? throw new ArgumentNullException(nameof(externalCacheService));
        private readonly StoredProcedureSecurityOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        private readonly ILogger<StoredProcedureAuthorizationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<bool> IsExecutionAllowedAsync(string databaseName, string procedureName, string tenantId, CancellationToken cancellationToken = default)
        {
            if (!_options.WhitelistEnabled)
            {
                _logger.LogDebug("Stored procedure whitelist disabled. Allowing execution for {ProcedureName}.", procedureName);
                return true;
            }

            var normalizedProcedureName = procedureName?.Trim();
            var normalizedTenantId = string.IsNullOrWhiteSpace(tenantId) ? "*" : tenantId.Trim();
            var cacheKey = $"sp-exec-policy:{databaseName}:{normalizedTenantId}:{normalizedProcedureName}";

            var cachedDecision = await _externalCacheService.GetAsync<bool?>(cacheKey, cancellationToken);
            if (cachedDecision.HasValue)
            {
                _logger.LogDebug("SP execution policy cache hit for {ProcedureName} (tenant: {TenantId}) -> {Decision}.", normalizedProcedureName, normalizedTenantId, cachedDecision.Value);
                return cachedDecision.Value;
            }

            try
            {
                _logger.LogDebug("SP execution policy cache miss for {ProcedureName} (tenant: {TenantId}). Loading from DB.", normalizedProcedureName, normalizedTenantId);
                var isAllowed = await LoadDecisionAsync(databaseName, normalizedProcedureName, normalizedTenantId, cancellationToken);
                await _externalCacheService.SetAsync(cacheKey, isAllowed, TimeSpan.FromSeconds(_options.CacheTtlSeconds), cancellationToken);
                _logger.LogDebug("SP execution policy cached for {ProcedureName} (tenant: {TenantId}) -> {Decision}.", normalizedProcedureName, normalizedTenantId, isAllowed);
                return isAllowed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stored procedure authorization lookup failed for {ProcedureName}", normalizedProcedureName);
                return _options.FailOpen;
            }
        }

        private async Task<bool> LoadDecisionAsync(string databaseName, string procedureName, string tenantId, CancellationToken cancellationToken)
        {
            await using var connection = await _sqlServerDatabase.GetOpenConnectionAsync(cancellationToken);

            var policyTable = $"[{_options.PolicyDatabase}].[{_options.PolicySchema}].[{_options.PolicyTable}]";

            var (schemaName, procName) = ParseSchemaAndProcedureName(procedureName);

            var sql = $@"
SELECT TOP (1) policy.AllowExecution
FROM {policyTable} policy
WHERE policy.SchemaName = @SchemaName
  AND policy.ProcedureName = @ProcedureName
  AND policy.IsEnabled = 1;";

            using var command = new SqlCommand(sql, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 15
            };

            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@ProcedureName", procName);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null && result != DBNull.Value && Convert.ToBoolean(result);
        }

        private static (string schemaName, string procedureName) ParseSchemaAndProcedureName(string procedureName)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                return ("dbo", string.Empty);
            }

            // Accept formats: schema.proc, [schema].[proc], dbo.proc
            var cleaned = procedureName.Trim().Replace("[", string.Empty).Replace("]", string.Empty);
            var parts = cleaned.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length >= 2)
            {
                // Support 2-part (schema.proc) and 3-part (db.schema.proc) names by taking the last two segments.
                return (parts[^2], parts[^1]);
            }

            return ("dbo", parts[0]);
        }
    }
}

