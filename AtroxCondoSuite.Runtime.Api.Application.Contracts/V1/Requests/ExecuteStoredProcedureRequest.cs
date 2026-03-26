namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Requests
{
    public class ExecuteStoredProcedureRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public List<ExecuteStoredProcedureParameterRequest> InputParameters { get; set; } = new();
    }
}

