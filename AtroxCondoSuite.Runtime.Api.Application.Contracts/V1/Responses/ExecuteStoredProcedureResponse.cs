namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Responses
{
    public class ExecuteStoredProcedureResponse
    {
        public bool Success { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public ExecuteStoredProcedureResultResponse Data { get; set; } = new();
        public List<ApiErrorResponse> Errors { get; set; } = new();
    }
}

