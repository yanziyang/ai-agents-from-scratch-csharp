using Microsoft.Extensions.Configuration;

namespace AiAgents.Core.Client;

public static class ConfigurationFactory
{
    public static IConfigurationRoot Create()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }
}
