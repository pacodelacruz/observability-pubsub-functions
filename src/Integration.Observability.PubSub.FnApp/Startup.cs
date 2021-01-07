using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Integration.Observability.PubSub.FnApp.Models;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(Integration.Observability.PubSub.FnApp.Startup))]
namespace Integration.Observability.PubSub.FnApp
{
    /// <summary>
    /// Implements the Options Pattern on Azure Functions here
    /// https://docs.microsoft.com/en-us/azure/architecture/serverless/code
    /// </summary>
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<FunctionOptions>()
                .Configure<IConfiguration>((configSection, configuration) =>
                { configuration.Bind(configSection); });
        }
    }
}
