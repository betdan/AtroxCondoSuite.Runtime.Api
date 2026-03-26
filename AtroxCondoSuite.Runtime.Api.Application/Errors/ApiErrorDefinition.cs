namespace AtroxCondoSuite.Runtime.Api.Application.Errors
{
    public sealed class ApiErrorDefinition(string code, string message)
    {
        public string Code { get; } = code;
        public string Message { get; } = message;
    }
}

