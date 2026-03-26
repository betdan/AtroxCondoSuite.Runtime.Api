namespace AtroxCondoSuite.Runtime.Api.Application.Errors
{
    using AtroxCondoSuite.Runtime.Api.Domain.Models.RequestResponse.Response;

    public static class ServiceResultFactory
    {
        public static ServiceResult Failure(string code, string message)
        {
            return new ServiceResult
            {
                Data = null,
                Error = [CreateError(code, message)]
            };
        }

        public static ErrorDto CreateError(string code, string message)
        {
            return new ErrorDto
            {
                Code = code,
                Message = message
            };
        }
    }
}

