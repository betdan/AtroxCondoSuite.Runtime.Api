using AtroxCondoSuite.Runtime.Api.Bootstrap;

var builder = LambdaApiBootstrap.CreateBuilder(args);
var app = LambdaApiBootstrap.ConfigurePipeline(builder.Build());
await app.RunAsync();






