namespace AtroxCondoSuite.Runtime.Api.Bootstrap
{
    using Amazon.Lambda.AspNetCoreServer.Hosting;
    using AtroxCondoSuite.Runtime.Api.Bootstrap.DependencyInjection;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Config;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Configuration;
    using AtroxCondoSuite.Runtime.Api.CrossCutting.Crypto;
    using AtroxCondoSuite.Runtime.Api.EntryPoints.Http.Middleware;
    using Prometheus;
    using Serilog;

    public static class LambdaApiBootstrap
    {
        public static WebApplicationBuilder CreateBuilder(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Host.UseSerilog(logger);
            builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);
            builder.Services.AddSingleton<Crypto>();
            builder.Services.AddControllers();

            builder.Services.Configure<ServiceRuntimeOptions>(configuration.GetSection(ServiceRuntimeOptions.SectionName));
            builder.Services.Configure<RdsSqlServerOptions>(configuration.GetSection(RdsSqlServerOptions.SectionName));
            builder.Services.Configure<AwsOptions>(configuration.GetSection(AwsOptions.SectionName));
            builder.Services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));
            builder.Services.Configure<ObservabilityOptions>(configuration.GetSection(ObservabilityOptions.SectionName));
            builder.Services.Configure<ExternalCacheOptions>(configuration.GetSection(ExternalCacheOptions.SectionName));
            builder.Services.Configure<StoredProcedureSecurityOptions>(configuration.GetSection(StoredProcedureSecurityOptions.SectionName));
            builder.Services.RegisterAtroxCondoSuiteServices(configuration);
            builder.Services.RegisterSwagger(configuration);

            var cacheOptions = configuration.GetSection(ExternalCacheOptions.SectionName).Get<ExternalCacheOptions>() ?? new ExternalCacheOptions();
            var cacheEndpoint = cacheOptions.Provider?.Equals("ElastiCache", StringComparison.OrdinalIgnoreCase) == true
                ? cacheOptions.ElastiCache.Configuration
                : cacheOptions.Redis.Configuration;
            Log.Debug("External cache provider: {Provider}. Endpoint: {Endpoint}.", cacheOptions.Provider, cacheEndpoint);

            Log.Information("Application Starting Up.");

            return builder;
        }

        public static WebApplication ConfigurePipeline(WebApplication app)
        {
            app.UseHttpsRedirection();
            SwaggerConfig.AddRegistration(app, app.Environment);
            app.UseMetricServer();
            app.UseHttpMetrics();
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            app.UseMiddleware<RequestCorrelationMiddleware>();
            app.UseMiddleware<RequestHeaderValidationMiddleware>();
            app.UseMiddleware<ResponseMetricMiddleware>();
            app.UseRouting();
            app.MapControllers();

            return app;
        }
    }
}

