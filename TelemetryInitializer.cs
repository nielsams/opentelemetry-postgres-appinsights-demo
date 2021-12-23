using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;

namespace PostgresTestApi
{
    internal class TelemetryInitializer : ITelemetryInitializer
    {
        private IConfiguration _config;
        public TelemetryInitializer(IConfiguration config)
        {
            _config = config;
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = _config.GetValue<string>("CloudRoleName");
        }
    }
}