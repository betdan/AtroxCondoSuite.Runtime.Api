namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Common
{
    public class ServiceRequest<T>
    {
        public T Body { get; set; } = default!;
    }
}

