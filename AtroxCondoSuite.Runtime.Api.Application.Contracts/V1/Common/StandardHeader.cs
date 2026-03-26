namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.V1.Common
{
    public class StandardHeader
    {
        public string? CorrelationId { get; set; }
        public string? TransactionId { get; set; }
        public string? SessionId { get; set; }
        public string? Channel { get; set; }
        public string? UserId { get; set; }
        public string? ClientIp { get; set; }
        public string? TraceId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}

