namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Requests
{
    public class ExecuteStoredProcedureParameterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}

