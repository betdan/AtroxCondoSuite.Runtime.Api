namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Responses
{
    public class ExecuteStoredProcedureResultResponse
    {
        public int Returns { get; set; }
        public List<string> Prints { get; set; } = new();
        public Dictionary<string, ExecuteStoredProcedureOutputParameterResponse> OutputParameters { get; set; } = new();
        public List<List<Dictionary<string, object>>> ResultSets { get; set; } = new();
    }
}

