namespace AtroxCondoSuite.Runtime.Api.DataAccess.Infrastructure.SqlServer
{
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Crypto;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Data.SqlClient;

    public class ConnectionStringBuilder(
        IOptions<RdsSqlServerOptions> options,
        ILogger<ConnectionStringBuilder> log,
        Crypto crypto)
    {
        private readonly RdsSqlServerOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        private readonly ILogger<ConnectionStringBuilder> _log = log ?? throw new ArgumentNullException(nameof(log));
        private readonly Crypto _crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));

        public string BuildSqlServerConnectionString()
        {
            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.Database))
            {
                _log.LogError("RDS SQL Server configuration is incomplete.");
            }

            var password = _options.Password;
            if (!string.IsNullOrWhiteSpace(password) && password.Contains("="))
            {
                try
                {
                    password = _crypto.Decrypt(password);
                }
                catch
                {
                    _log.LogDebug("RDS password could not be decrypted; the configured value will be used as-is.");
                }
            }

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = $"{_options.Host},{_options.Port}",
                InitialCatalog = _options.Database,
                UserID = _options.Username,
                Password = password,
                Encrypt = _options.Encrypt,
                TrustServerCertificate = _options.TrustServerCertificate,
                ConnectTimeout = _options.ConnectionTimeoutSeconds,
                ApplicationName = _options.ApplicationName
            };

            _log.LogInformation("RDS SQL Server connection string prepared for Host={Host}, Database={Database}", _options.Host, _options.Database);

            return builder.ConnectionString;
        }
    }
}





