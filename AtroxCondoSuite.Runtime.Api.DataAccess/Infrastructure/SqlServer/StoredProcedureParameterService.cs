namespace AtroxCondoSuite.Runtime.Api.DataAccess.Infrastructure.SqlServer
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Caching;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.DataAccess;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public sealed class StoredProcedureParameterService(
        IExternalCacheService externalCacheService,
        IOptions<ExternalCacheOptions> cacheOptions,
        ILogger<StoredProcedureParameterService> logger)
    {
        private readonly IExternalCacheService _externalCacheService = externalCacheService ?? throw new ArgumentNullException(nameof(externalCacheService));
        private readonly ExternalCacheOptions _cacheOptions = cacheOptions?.Value ?? new ExternalCacheOptions();
        private readonly ILogger<StoredProcedureParameterService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Security",
            "CA2100:Review SQL queries for security vulnerabilities",
            Justification = "The command only executes a fixed metadata query and does not accept executable SQL from users.")]
        public async Task<(List<Parameter>, SqlException)> GetStoredProcedureParametersAsync(SqlConnection connection, string databaseName, string procedureName)
        {
            SqlException sqlException = null;

            var normalizedProcedureName = procedureName?.Trim();
            var cacheKey = $"sp-params:{databaseName}:{normalizedProcedureName}";
            var cachedParameters = await _externalCacheService.GetAsync<List<Parameter>>(cacheKey);
            if (cachedParameters is not null)
            {
                _logger.LogDebug("SP parameters cache hit for {Database}.{Procedure} (count: {Count}).", databaseName, normalizedProcedureName, cachedParameters.Count);
                return (cachedParameters, null);
            }

            _logger.LogDebug("SP parameters cache miss for {Database}.{Procedure}.", databaseName, normalizedProcedureName);
            connection.ChangeDatabase(databaseName);

            const string sql = @"
                SELECT 
                    p.name AS ParameterName, 
                    t.name AS DataType, 
                    p.max_length AS Length, 
                    p.is_output AS IsOutput,
                    '' AS DefaultValue
                FROM sys.parameters p
                JOIN sys.types t ON p.user_type_id = t.user_type_id
                WHERE p.object_id = OBJECT_ID(@ProcedureName, 'P')
                ORDER BY p.parameter_id";

            var parameters = new List<Parameter>();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@ProcedureName", procedureName));
            command.CommandTimeout = 30;

            try
            {
                await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    parameters.Add(new Parameter
                    {
                        ParameterName = reader["ParameterName"].ToString(),
                        DataTypes = [reader["DataType"].ToString()],
                        Length = Convert.ToInt32(reader["Length"]),
                        IsOutput = Convert.ToBoolean(reader["IsOutput"]),
                        DefaultValue = reader["DefaultValue"].ToString()
                    });
                }
            }
            catch (SqlException ex)
            {
                sqlException = ex;
            }

            if (sqlException is null)
            {
                var ttl = TimeSpan.FromSeconds(_cacheOptions.DefaultTtlSeconds);
                await _externalCacheService.SetAsync(cacheKey, parameters, ttl);
                _logger.LogDebug("SP parameters cached for {Database}.{Procedure} (count: {Count}, ttl: {TtlSeconds}s).", databaseName, normalizedProcedureName, parameters.Count, _cacheOptions.DefaultTtlSeconds);
            }

            return (parameters, sqlException);
        }
    }
}

