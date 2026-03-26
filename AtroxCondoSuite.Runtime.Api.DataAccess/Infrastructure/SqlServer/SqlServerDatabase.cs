namespace AtroxCondoSuite.Runtime.Api.DataAccess.Infrastructure.SqlServer
{
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration;
    using AtroxCondoSuite.Runtime.Api.DataAccess.Contracts.Connections;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Options;
    using System.Data;
    using System.Text;

    public class SqlServerDatabase(
        ConnectionStringBuilder connectionStringBuilder,
        StoredProcedureParameterService storedProcedureParameterService,
        IOptions<RdsSqlServerOptions> options) : IDisposable, ISqlServerDatabase
    {
        private readonly string _connectionString = connectionStringBuilder.BuildSqlServerConnectionString() ?? throw new InvalidOperationException("Could not build the SQL Server connection string.");
        private readonly StoredProcedureParameterService _storedProcedureParameterService = storedProcedureParameterService ?? throw new ArgumentNullException(nameof(storedProcedureParameterService));
        private readonly RdsSqlServerOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        private SqlConnection _connection;

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Security",
            "CA2100:Review SQL queries for security vulnerabilities",
            Justification = "This command only executes stored procedures with controlled parameters.")]
        public async Task<(List<DataTable> resultSets, Dictionary<string, object> outputParameters, List<string> printMessages, int returnValue, Dictionary<int, string> rErrors)> ExecuteStoredProcedureAsync(string databaseName, string procedureName, Dictionary<string, object> inputParams = null, CancellationToken cancellationToken = default)
        {
            var resultSets = new List<DataTable>();
            var outputValues = new Dictionary<string, object>();
            var printMessages = new List<string>();
            var raisedErrors = new Dictionary<int, string>();
            var returnValue = 0;

            await using var connection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            connection.InfoMessage += (_, e) =>
            {
                foreach (SqlError error in e.Errors)
                {
                    var message = new StringBuilder(error.Message);

                    if (!string.IsNullOrEmpty(error.Procedure))
                    {
                        message.Append($" (Procedure: {error.Procedure})");
                    }

                    if (error.LineNumber > 0)
                    {
                        message.Append($" (Line: {error.LineNumber})");
                    }

                    if (!message.ToString().Contains("Changed database", StringComparison.OrdinalIgnoreCase))
                    {
                        printMessages.Add(message.ToString());
                    }
                }
            };

            var (parameters, parameterDiscoveryError) = await _storedProcedureParameterService.GetStoredProcedureParametersAsync(connection, databaseName, procedureName).ConfigureAwait(false);

            if (parameterDiscoveryError != null)
            {
                foreach (SqlError sqlError in parameterDiscoveryError.Errors)
                {
                    if (sqlError.Number <= 0)
                    {
                        continue;
                    }

                    var message = new StringBuilder().Append($"({sqlError.Number}) {sqlError.Message}");

                    if (!string.IsNullOrEmpty(sqlError.Procedure))
                    {
                        message.Append($" (Procedure: {sqlError.Procedure})");
                    }

                    if (sqlError.LineNumber > 0)
                    {
                        message.Append($" (Line: {sqlError.LineNumber})");
                    }

                    raisedErrors[sqlError.Number] = message.ToString();
                }

                return (resultSets, outputValues, printMessages, returnValue, raisedErrors);
            }

            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _options.CommandTimeoutSeconds
            };

            command.Parameters.Add(new SqlParameter("@ReturnValue", SqlDbType.Int)
            {
                Direction = ParameterDirection.ReturnValue
            });

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    if (parameter.IsOutput)
                    {
                        var sqlParameter = new SqlParameter(parameter.ParameterName, SqlDbTypeMapper.Map(parameter.DataTypes.FirstOrDefault()))
                        {
                            Direction = ParameterDirection.Output,
                            Size = parameter.Length > 0 ? parameter.Length : 0
                        };

                        if (!string.IsNullOrEmpty(parameter.DefaultValue))
                        {
                            sqlParameter.Value = parameter.DefaultValue;
                        }

                        command.Parameters.Add(sqlParameter);
                        continue;
                    }

                    if (inputParams == null)
                    {
                        continue;
                    }

                    var inputParameter = inputParams.FirstOrDefault(p => p.Key.Equals(parameter.ParameterName, StringComparison.OrdinalIgnoreCase));

                    if (inputParameter.Key == null)
                    {
                        continue;
                    }

                    var mappedDbType = SqlDbTypeMapper.Map(parameter.DataTypes.FirstOrDefault());
                    var convertedValue = SqlDbTypeConverter.ConvertToSqlDbType(inputParameter.Value, mappedDbType);
                    command.Parameters.AddWithValue(parameter.ParameterName, convertedValue ?? DBNull.Value);
                }
            }

            try
            {
                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                do
                {
                    if (reader.IsClosed || reader.FieldCount == 0)
                    {
                        break;
                    }

                    var dataTable = new DataTable();
                    dataTable.Load(reader);
                    resultSets.Add(dataTable);
                }
                while (!reader.IsClosed && await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
            }
            catch (SqlException ex)
            {
                foreach (SqlError error in ex.Errors)
                {
                    raisedErrors[error.Number] = error.Message;
                }

                return (resultSets, outputValues, printMessages, returnValue, raisedErrors);
            }

            if (parameters != null)
            {
                foreach (var parameter in parameters.Where(p => p.IsOutput))
                {
                    var parameterValue = command.Parameters[parameter.ParameterName].Value;
                    var parameterType = command.Parameters[parameter.ParameterName].SqlDbType.ToString();
                    outputValues[parameter.ParameterName] = new { Value = parameterValue, Type = parameterType };
                }
            }

            returnValue = (int)command.Parameters["@ReturnValue"].Value;

            return (resultSets, outputValues, printMessages, returnValue, raisedErrors);
        }

        public async Task<SqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = new SqlConnection(_connectionString);
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            return _connection;
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }
    }
}

