namespace AtroxCondoSuite.Runtime.Api.Bootstrap.DependencyInjection
{
    using Amazon.Extensions.NETCore.Setup;
    using Amazon.SimpleNotificationService;
    using Amazon.SQS;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Messaging;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Caching;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Security;
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Services;
    using AtroxCondoSuite.Runtime.Api.Application.Services;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Metrics;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Observability;
    using AtroxCondoSuite.Runtime.Api.DataAccess.Contracts.Connections;
    using AtroxCondoSuite.Runtime.Api.DataAccess.Infrastructure.SqlServer;
    using AtroxCondoSuite.Runtime.Api.DataAccess.Infrastructure.SqlServer.Executions;
    using AtroxCondoSuite.Runtime.Api.EntryPoints.Sqs.Handlers;
    using AtroxCondoSuite.Runtime.Api.Infrastructure.Caching;
    using AtroxCondoSuite.Runtime.Api.Infrastructure.Aws.Sns;
    using AtroxCondoSuite.Runtime.Api.Infrastructure.Security;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Options;
    using Microsoft.OpenApi.Models;
    using Prometheus.SystemMetrics;

    public static class ServiceCollectionExtensions
    {
        public static void RegisterAtroxCondoSuiteServices(this IServiceCollection services, IConfiguration configuration)
        {
            var externalCacheOptions = configuration.GetSection(ExternalCacheOptions.SectionName).Get<ExternalCacheOptions>() ?? new ExternalCacheOptions();

            services.AddDefaultAWSOptions(configuration.GetAWSOptions(AwsOptions.SectionName));
            services.AddAWSService<IAmazonSimpleNotificationService>();
            services.AddAWSService<IAmazonSQS>();

            RegisterExternalCache(services, externalCacheOptions);
            services.AddSystemMetrics();

            services.AddSingleton<AtroxCondoSuiteDiagnostics>();
            services.AddSingleton<MetricCollector>();

            services.AddScoped<ConnectionStringBuilder>();
            services.AddScoped<StoredProcedureParameterService>();
            services.AddScoped<ISqlServerDatabase, SqlServerDatabase>();
            services.AddScoped<AtroxCondoSuiteApiExecute>();
            services.AddScoped<IStoredProcedureAuthorizationService, StoredProcedureAuthorizationService>();
            services.AddScoped<IAtroxCondoSuiteApiServices, AtroxCondoSuiteApiServices>();
            services.AddScoped<IAtroxCondoSuiteQueueProcessor, AtroxCondoSuiteQueueProcessor>();
            services.AddScoped<IAtroxCondoSuiteSqsMessageHandler, AtroxCondoSuiteSqsMessageHandler>();
            services.AddScoped<IAtroxCondoSuiteNotificationPublisher, SnsNotificationPublisher>();
        }

        private static void RegisterExternalCache(IServiceCollection services, ExternalCacheOptions options)
        {
            if (string.Equals(options.Provider, "Redis", StringComparison.OrdinalIgnoreCase))
            {
                services.AddStackExchangeRedisCache(redisOptions =>
                {
                    redisOptions.Configuration = options.Redis.Configuration;
                    redisOptions.InstanceName = options.Redis.InstanceName;
                });
                services.AddSingleton<IExternalCacheService, DistributedExternalCacheService>();
                return;
            }

            if (string.Equals(options.Provider, "ElastiCache", StringComparison.OrdinalIgnoreCase))
            {
                services.AddStackExchangeRedisCache(redisOptions =>
                {
                    redisOptions.Configuration = options.ElastiCache.Configuration;
                    redisOptions.InstanceName = options.ElastiCache.InstanceName;
                });
                services.AddSingleton<IExternalCacheService, DistributedExternalCacheService>();
                return;
            }

            services.AddSingleton<IExternalCacheService, NullExternalCacheService>();
        }

        public static void RegisterSwagger(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "AtroxCondoSuite.Runtime.Api",
                    Description = "Atrox CondoSuite API for Lambda, SQS, SNS, and SQL Server integration.",
                    TermsOfService = new Uri(configuration["ExternalLinks:TermsOfService"]),
                    Contact = new OpenApiContact
                    {
                        Name = "Integration Support",
                        Email = "support@atroxcondosuite.com",
                        Url = new Uri(configuration["ExternalLinks:IntegrationSupport"])
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Commercial License",
                        Url = new Uri(configuration["ExternalLinks:License"])
                    }
                });
            });
        }
    }
}

