namespace AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration
{
    public sealed class ObservabilityOptions
    {
        public const string SectionName = "Observability";

        public bool EnablePrometheus { get; set; } = true;
        public bool EnableRequestMetrics { get; set; } = true;
        public bool IncludeSensitiveDataInLogs { get; set; }
    }
}

