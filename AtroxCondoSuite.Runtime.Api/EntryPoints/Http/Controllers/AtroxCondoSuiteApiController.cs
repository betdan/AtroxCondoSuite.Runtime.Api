namespace AtroxCondoSuite.Runtime.Api.EntryPoints.Http.Controllers
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Services;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Common;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Requests;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Responses;
    using AtroxCondoSuite.Runtime.Api.EntryPoints.Http.Mappers;
    using AtroxCondoSuite.Runtime.Api.EntryPoints.Http.Middleware;
    using Microsoft.AspNetCore.Mvc;
    using System.Net;

    [Route("api/v1/[controller]")]
    [ApiController]
    public class AtroxCondoSuiteApiController(ILogger<AtroxCondoSuiteApiController> logger, IAtroxCondoSuiteApiServices app) : ControllerBase
    {
        private readonly ILogger<AtroxCondoSuiteApiController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAtroxCondoSuiteApiServices _app = app ?? throw new ArgumentNullException(nameof(app));

        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ServiceResponse<ExecuteStoredProcedureResultResponse>))]
        [Produces("application/json")]
        [HttpPost]
        public async Task<IActionResult> DefaultRoute([FromBody] ServiceRequest<ExecuteStoredProcedureRequest> request)
        {
            if (request?.Body == null)
            {
                return BadRequest(new ServiceResponse<ExecuteStoredProcedureResultResponse>
                {
                    Success = false,
                    Header = new StandardHeader { TraceId = HttpContext.TraceIdentifier, Timestamp = DateTimeOffset.UtcNow },
                    Code = "INVALID_REQUEST",
                    Message = "Request body is required.",
                    Errors = new Dictionary<string, string[]>
                    {
                        ["body"] = ["The request must include a body with the stored procedure definition."]
                    },
                    Data = null
                });
            }

            var applicationDto = request.ToApplicationDto(HttpContext);

            _logger.LogInformation("HTTP request received for procedure {ProcedureName}", applicationDto?.Data?.procedureName);

            var response = await _app.ExecuteAsync(applicationDto, HttpContext.RequestAborted);

            _logger.LogInformation("HTTP request completed for procedure {ProcedureName} with {ErrorCount} errors", applicationDto?.Data?.procedureName, response.Error?.Count ?? 0);

            var standardHeader = HttpContext.Items.TryGetValue(RequestHeaderValidationMiddleware.StandardHeaderKey, out var headerValue)
                ? headerValue as StandardHeader
                : new StandardHeader { TraceId = HttpContext.TraceIdentifier, Timestamp = DateTimeOffset.UtcNow };

            return Ok(response.ToContract(standardHeader));
        }

        [HttpPost("{extraValue}")]
        public async Task<IActionResult> HandleExtraValue([FromBody] ServiceRequest<ExecuteStoredProcedureRequest> request, string extraValue)
        {
            if (request?.Body == null)
            {
                return BadRequest(new ServiceResponse<ExecuteStoredProcedureResultResponse>
                {
                    Success = false,
                    Header = new StandardHeader { TraceId = HttpContext.TraceIdentifier, Timestamp = DateTimeOffset.UtcNow },
                    Code = "INVALID_REQUEST",
                    Message = "Request body is required.",
                    Errors = new Dictionary<string, string[]>
                    {
                        ["body"] = ["The request must include a body with the stored procedure definition."]
                    },
                    Data = null
                });
            }

            var applicationDto = request.ToApplicationDto(HttpContext);

            _logger.LogInformation("Received route value {ExtraValue}", extraValue);
            _logger.LogInformation("HTTP request received for procedure {ProcedureName}", applicationDto?.Data?.procedureName);

            var response = await _app.ExecuteAsync(applicationDto, HttpContext.RequestAborted);

            _logger.LogInformation("HTTP request completed for procedure {ProcedureName} with {ErrorCount} errors", applicationDto?.Data?.procedureName, response.Error?.Count ?? 0);

            var standardHeader = HttpContext.Items.TryGetValue(RequestHeaderValidationMiddleware.StandardHeaderKey, out var headerValue)
                ? headerValue as StandardHeader
                : new StandardHeader { TraceId = HttpContext.TraceIdentifier, Timestamp = DateTimeOffset.UtcNow };

            return Ok(response.ToContract(standardHeader));
        }
    }
}

