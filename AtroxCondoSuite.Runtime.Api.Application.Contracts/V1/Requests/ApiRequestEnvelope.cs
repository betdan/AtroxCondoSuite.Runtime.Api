namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Requests
{
    public class ApiRequestEnvelope<T>
    {
        public T Body { get; set; } = default!;
    }
}

