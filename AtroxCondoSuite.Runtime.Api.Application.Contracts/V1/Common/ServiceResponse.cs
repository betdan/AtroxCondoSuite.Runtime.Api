namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Common
{
    public class ServiceResponse<T>
    {
        public bool Success { get; set; }
        public StandardHeader Header { get; set; } = new();
        public T Data { get; set; } = default!;
        public string? Code { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, string[]> Errors { get; set; } = new();
    }
}

