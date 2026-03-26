namespace AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration
{
    public sealed class RdsSqlServerOptions
    {
        public const string SectionName = "RdsSqlServer";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 1433;
        public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public int CommandTimeoutSeconds { get; set; } = 60;
        public bool Encrypt { get; set; } = true;
        public bool TrustServerCertificate { get; set; } = true;
        public string ApplicationName { get; set; } = "AtroxCondoSuite.Runtime.Api";
    }
}

