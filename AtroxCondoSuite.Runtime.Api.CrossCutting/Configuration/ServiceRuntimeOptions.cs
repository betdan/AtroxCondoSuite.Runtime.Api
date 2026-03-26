namespace AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration
{
    public sealed class ServiceRuntimeOptions
    {
        public const string SectionName = "ServiceRuntime";

        public string Name { get; set; } = "AtroxCondoSuite.Runtime.Api";
        public string AppType { get; set; } = "AWS Lambda";
        public string Environment { get; set; } = "Development";
    }
}

