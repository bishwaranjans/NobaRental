using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Hosting;

namespace NobaRental.Shared.ServiceDefaults;

public static class HostEnvironmentExtensions
{
    public static TokenCredential GetTokenCredentials(this IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return new DefaultAzureCredential();

        // ChainedTokenCredential should be the fastest option for local environment
        // https://anthonysimmon.com/defaultazurecredential-local-development-optimization/
        return new ChainedTokenCredential(
            new AzureCliCredential(),
            new DefaultAzureCredential());
    }
}