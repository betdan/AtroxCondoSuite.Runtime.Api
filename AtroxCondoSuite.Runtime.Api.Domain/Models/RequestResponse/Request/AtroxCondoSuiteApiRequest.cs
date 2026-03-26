namespace AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Request
{
    public class AtroxCondoSuiteApiRequest
    {
        public String tenantId { get; set; }
        public String databaseName { get; set; }
        public String procedureName { get; set; }
        public List<InputParamsRequest> inputParameters { get; set; }
    }
}





