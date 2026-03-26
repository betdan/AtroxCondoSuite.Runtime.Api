namespace AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration
{
    public sealed class ExternalCacheOptions
    {
        public const string SectionName = "ExternalCache";

        public string Provider { get; set; } = "None";

        public int DefaultTtlSeconds { get; set; } = 300;

        public RedisCacheEndpointOptions Redis { get; set; } = new();

        public RedisCacheEndpointOptions ElastiCache { get; set; } = new();
    }

    public sealed class RedisCacheEndpointOptions
    {
        public string Configuration { get; set; } = string.Empty;

        public string InstanceName { get; set; } = "AtroxCondoSuite:";
    }
}

