using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PostgresTestApi.Postgres;
using System.Text.RegularExpressions;

namespace PostgresTestApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton<IPostgresClient>(provider =>
            {
                return new PostgresClient(Configuration);
            });

            // The only reason we're adding a custom telemetry initializer, is because we explicitly want to set the CloudRoleName.
            services.AddSingleton<ITelemetryInitializer>(provider => 
            {
                return new TelemetryInitializer(Configuration);
            });

            // We need the key, but have the connection string in a variable because OT expects that. 
            // Instead of adding a separate variable with only the key, we extract it from the connectionstring
            string appInsightsConnectionString = Configuration.GetValue<string>("AppInsights_ConnectionString");
            string appInsightsKey = Regex.Match(appInsightsConnectionString, @"(InstrumentationKey=)([a-f0-9-]{36})(\S*)").Groups[2].Value;

            // This is the 'regular' AppInsights activation, regardless of OT. 
            // We're keeping this in as well because it gives us many insights that are not sent through the OT channel.
            services.AddApplicationInsightsTelemetry(appInsightsKey);

            // Get the intended Role Name for this service from config:
            // We're using this both in the TelemetryInitializer for the AppInsights SDK as well as the OT tracing.
            // By using the same name in both channels, we make sure that the applicationmap shows this service only once. 
            string roleName = Configuration.GetValue<string>("CloudRoleName");

            // The following statements add OpenTelemetry tracing to the postgres dependency. 
            // This gives us the Postgres tracing we're after but lacks LiveMetrics, custom metrics, etc.
            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                // the name you use in AddService will show up in the AppInsights appmap:
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(roleName))

                // The AlwaysOn sampler always sends every trace upstream.
                // Consider using a different sampler in high-volume production environments. 
                .SetSampler(new AlwaysOnSampler())

                // Activate NpgSQL tracing in OT
                .AddNpgsql()
                
                // Output trace data to the console. Useful to get started. 
                .AddConsoleExporter()

                // This sends tracing data to ApplicationInsights.
                .AddAzureMonitorTraceExporter(o =>
                {
                    // Add the AppInsights connection string to the exporter
                    o.ConnectionString = appInsightsConnectionString;
                })
                .Build();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }
}
