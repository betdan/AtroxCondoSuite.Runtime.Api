namespace AtroxCondoSuite.Runtime.Api.CrossCutting.Config
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Hosting;

    public static class SwaggerConfig
    {
        public static IApplicationBuilder AddRegistration(this IApplicationBuilder app, IHostEnvironment environment)
        {
            if (!environment.IsDevelopment())
            {
                return app;
            }

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "AtroxCondoSuite.Runtime.Api v1");
                options.DefaultModelsExpandDepth(-1);
            });

            return app;
        }
    }
}

