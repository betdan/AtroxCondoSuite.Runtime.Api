namespace AtroxCondoSuite.Runtime.Api.DataAccess.Infrastructure.SqlServer.Executions
{
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Request;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Observability;
    using AtroxCondoSuite.Runtime.Api.DataAccess.Contracts.Connections;
    using Microsoft.Extensions.Logging;
    using System.Data;

    public class AtroxCondoSuiteApiExecute(
        ISqlServerDatabase database,
        ILogger<AtroxCondoSuiteApiExecute> logger,
        AtroxCondoSuiteDiagnostics diagnostics)
    {
        private readonly ISqlServerDatabase _database = database ?? throw new ArgumentNullException(nameof(database));
        private readonly ILogger<AtroxCondoSuiteApiExecute> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly AtroxCondoSuiteDiagnostics _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        public async Task<AtroxCondoSuiteApiResponse> ExecuteAsync(ApplicationDto request, CancellationToken cancellationToken = default)
        {
            if (request?.Data == null)
            {
                throw new ArgumentNullException(nameof(request), "The request payload cannot be null.");
            }

            using var activity = _diagnostics.ActivitySource.StartActivity("sqlserver.execute-procedure");

            var response = new AtroxCondoSuiteApiResponse
            {
                returns = 0,
                prints = new List<string>(),
                raisError = new List<RaisError>(),
                outputParameters = new Dictionary<string, ParameterValue>(),
                resultSets = new List<ResultSets>()
            };

            var inputParams = new Dictionary<string, object>();

            foreach (var parameter in request.Data.inputParameters ?? Enumerable.Empty<InputParamsRequest>())
            {
                if (!string.IsNullOrWhiteSpace(parameter.paramName) && parameter.value != null)
                {
                    inputParams[parameter.paramName] = parameter.value;
                }
            }

            activity?.SetTag("db.system", "sqlserver");
            activity?.SetTag("db.name", request.Data.databaseName);
            activity?.SetTag("db.operation", request.Data.procedureName);
            activity?.SetTag("db.input_parameter_count", inputParams.Count);

            try
            {
                var (resultSets, outputValues, printMessages, returnValue, errors) =
                    await _database.ExecuteStoredProcedureAsync(request.Data.databaseName, request.Data.procedureName, inputParams, cancellationToken);

                response.returns = returnValue;
                response.prints = printMessages ?? new List<string>();

                // 1) SQL/transport errors (exceptions)
                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        response.raisError.Add(new RaisError
                        {
                            code = error.Key,
                            message = error.Value
                        });
                    }
                }

                foreach (var outputParam in outputValues ?? new Dictionary<string, object>())
                {
                    var parameterDetails = outputParam.Value as dynamic;

                    response.outputParameters[outputParam.Key] = new ParameterValue
                    {
                        Value = parameterDetails?.Value?.ToString() ?? string.Empty,
                        Type = parameterDetails?.Type ?? string.Empty
                    };
                }

                // 2) Business-level SP outputs (@o_error / @o_ErrorDescription)
                var oErrorKey = response.outputParameters.Keys.FirstOrDefault(k => k.Equals("@o_error", StringComparison.OrdinalIgnoreCase));
                var oErrorDescriptionKey = response.outputParameters.Keys.FirstOrDefault(k => k.Equals("@o_ErrorDescription", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(oErrorKey) &&
                    int.TryParse(response.outputParameters[oErrorKey].Value, out var oError) &&
                    oError != 0)
                {
                    var message = !string.IsNullOrWhiteSpace(oErrorDescriptionKey)
                        ? response.outputParameters[oErrorDescriptionKey].Value
                        : "Stored procedure returned an error.";

                    response.raisError.Add(new RaisError
                    {
                        code = oError,
                        message = message
                    });
                }

                foreach (var table in resultSets ?? new List<DataTable>())
                {
                    var resultSet = new ResultSets
                    {
                        ResultSet = new List<Dictionary<string, object>>()
                    };

                    foreach (DataRow row in table.Rows)
                    {
                        var rowDictionary = new Dictionary<string, object>();

                        foreach (DataColumn column in table.Columns)
                        {
                            rowDictionary[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                        }

                        resultSet.ResultSet.Add(rowDictionary);
                    }

                    response.resultSets.Add(resultSet);
                }

                _diagnostics.DatabaseCallsTotal.Inc();
                _diagnostics.StoredProcedureResultSets.Observe(resultSets?.Count ?? 0);
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                _diagnostics.DatabaseFailuresTotal.Inc();
                _logger.LogError(ex, "Error executing stored procedure {ProcedureName} on database {DatabaseName}", request.Data.procedureName, request.Data.databaseName);
                response.raisError.Add(new RaisError
                {
                    code = -1,
                    message = "Unhandled error executing the stored procedure."
                });
            }

            return response;
        }
    }
}

