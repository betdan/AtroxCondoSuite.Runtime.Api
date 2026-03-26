namespace AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration
{
    public sealed class StoredProcedureSecurityOptions
    {
        public const string SectionName = "StoredProcedureSecurity";

        public bool WhitelistEnabled { get; set; } = true;

        public bool FailOpen { get; set; }

        public string PolicyDatabase { get; set; } = "AtroxCondoSuiteDB";

        public string PolicySchema { get; set; } = "security";

        public string PolicyTable { get; set; } = "StoredProcedureExecutionPolicies";

        public int CacheTtlSeconds { get; set; } = 300;
    }
}

