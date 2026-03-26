namespace AtroxCondoSuite.Runtime.Api.EntryPoints.Http.Mappers
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Common;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Requests;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Responses;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Request;
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;
    using Microsoft.AspNetCore.Http;
    using System.Security.Claims;

    public static class ApiContractMapper
    {
        public static ApplicationDto ToApplicationDto(this ServiceRequest<ExecuteStoredProcedureRequest> request, HttpContext httpContext)
        {
            var header = ResolveHeader(httpContext);
            var transactionId = ReadGuidOrNew(header?.TransactionId);
            var sessionId = ReadGuidOrNew(header?.SessionId);
            var userId = string.IsNullOrWhiteSpace(header?.UserId) ? ResolveUserId(httpContext) : header.UserId;
            var clientIp = string.IsNullOrWhiteSpace(header?.ClientIp) ? ReadClientIp(httpContext) ?? "0.0.0.0" : header.ClientIp;
            var channel = string.IsNullOrWhiteSpace(header?.Channel) ? "HTTP" : header.Channel;

            var inputParameters = request?.Body?.InputParameters?.Select(parameter => new InputParamsRequest
            {
                paramName = parameter.Name,
                value = parameter.Value
            }).ToList() ?? [];

            // Mandatory execution headers for all stored procedures (provided by the runtime server).
            inputParameters.Insert(0, new InputParamsRequest { paramName = "@i_transaction_id", value = transactionId.ToString() });
            inputParameters.Insert(1, new InputParamsRequest { paramName = "@i_session_id", value = sessionId.ToString() });
            inputParameters.Insert(2, new InputParamsRequest { paramName = "@i_channel", value = channel });
            inputParameters.Insert(3, new InputParamsRequest { paramName = "@i_user_id", value = userId });
            inputParameters.Insert(4, new InputParamsRequest { paramName = "@i_client_ip", value = clientIp });

            return new ApplicationDto
            {
                Data = new AtroxCondoSuiteApiRequest
                {
                    tenantId = request?.Body?.TenantId,
                    databaseName = request?.Body?.DatabaseName,
                    procedureName = request?.Body?.ProcedureName,
                    inputParameters = inputParameters
                }
            };
        }

        public static ServiceResponse<ExecuteStoredProcedureResultResponse> ToContract(this ServiceResult result, StandardHeader header)
        {
            var normalizedHeader = NormalizeHeader(header);
            var hasErrors = result?.Error != null && result.Error.Count > 0;

            return new ServiceResponse<ExecuteStoredProcedureResultResponse>
            {
                Success = !hasErrors,
                Header = normalizedHeader,
                Data = result?.Data == null
                    ? null
                    : new ExecuteStoredProcedureResultResponse
                    {
                        Returns = result.Data.returns,
                        Prints = result.Data.prints,
                        OutputParameters = result.Data.outputParameters?.ToDictionary(
                            item => item.Key,
                            item => new ExecuteStoredProcedureOutputParameterResponse
                            {
                                Value = item.Value.Value,
                                Type = item.Value.Type
                            }),
                        ResultSets = result.Data.resultSets?.Select(resultSet => resultSet.ResultSet).ToList()
                    },
                Code = hasErrors ? result?.Error?.FirstOrDefault()?.Code ?? "EXECUTION_ERROR" : null,
                Message = hasErrors ? "One or more errors occurred." : null,
                Errors = hasErrors
                    ? new Dictionary<string, string[]>
                    {
                        ["errors"] = result?.Error?.Select(e => $"{e.Code}: {e.Message}").ToArray() ?? Array.Empty<string>()
                    }
                    : new Dictionary<string, string[]>()
            };
        }

        private static StandardHeader ResolveHeader(HttpContext httpContext)
        {
            if (httpContext != null && httpContext.Items.TryGetValue("standard-header", out var headerObj))
            {
                if (headerObj is StandardHeader header)
                {
                    return header;
                }
            }

            return new StandardHeader
            {
                CorrelationId = httpContext?.Response?.Headers["x-correlation-id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N"),
                TransactionId = Guid.NewGuid().ToString("N"),
                SessionId = Guid.NewGuid().ToString("N"),
                Channel = "HTTP",
                UserId = ResolveUserId(httpContext),
                ClientIp = ReadClientIp(httpContext) ?? "0.0.0.0",
                TraceId = httpContext?.TraceIdentifier,
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        private static Guid ReadGuidOrNew(string value)
        {
            return Guid.TryParse(value, out var parsed) ? parsed : Guid.NewGuid();
        }

        private static string ResolveUserId(HttpContext httpContext)
        {
            return httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? httpContext?.User?.FindFirstValue("sub")
                   ?? "anonymous";
        }

        private static StandardHeader NormalizeHeader(StandardHeader header)
        {
            if (header == null)
            {
                return new StandardHeader
                {
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    TransactionId = Guid.NewGuid().ToString("N"),
                    SessionId = Guid.NewGuid().ToString("N"),
                    Channel = "HTTP",
                    UserId = "anonymous",
                    ClientIp = "0.0.0.0",
                    TraceId = null,
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            header.CorrelationId ??= Guid.NewGuid().ToString("N");
            header.TransactionId ??= Guid.NewGuid().ToString("N");
            header.SessionId ??= Guid.NewGuid().ToString("N");
            header.Channel ??= "HTTP";
            header.UserId ??= "anonymous";
            header.ClientIp ??= "0.0.0.0";
            header.Timestamp = DateTimeOffset.UtcNow;
            return header;
        }

        private static string ReadClientIp(HttpContext httpContext)
        {
            var forwarded = httpContext?.Request?.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var first = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(first))
                {
                    return first;
                }
            }

            return httpContext?.Connection?.RemoteIpAddress?.ToString();
        }
    }
}

