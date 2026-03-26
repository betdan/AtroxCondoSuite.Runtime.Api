namespace AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response
{
    public class ServiceResult
    {
        public AtroxCondoSuiteApiResponse Data { get; set; }
        public List<ErrorDto> Error { get; set; }
    }
}





