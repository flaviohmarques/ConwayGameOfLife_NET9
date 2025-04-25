using Serilog;
namespace ConwayGameOfLife_NET9.Configurations;

public static class LoggingSetup
{
    public static IHostBuilder UseLoggingSetup(this IHostBuilder host, IConfiguration configuration)
    {
        host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration.ReadFrom.Configuration(configuration);
        });

        return host;
    }
}