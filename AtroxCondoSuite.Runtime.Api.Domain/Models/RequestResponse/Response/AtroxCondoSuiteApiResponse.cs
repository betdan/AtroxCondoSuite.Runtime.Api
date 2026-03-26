
namespace AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response
{
    public class AtroxCondoSuiteApiResponse
    {
        public int returns { get; set; }
        public List<string> prints { get; set; }
        public List<RaisError> raisError { get; set; }
        public Dictionary<string, ParameterValue> outputParameters { get; set; }
        public List<ResultSets> resultSets { get; set; }
    }
}




