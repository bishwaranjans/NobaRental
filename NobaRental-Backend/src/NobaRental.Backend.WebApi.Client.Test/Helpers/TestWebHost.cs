using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NobaRental.Backend.WebApi.Client.Test.Helpers;

public abstract class TestWebHost : WebApplicationFactory<Program>
{
    private readonly List<ServiceDescriptor> _serviceDescriptors = [];
    private HttpClient? _client;

    protected HttpClient GetClient()
    {
        if (_client is not null)
        {
            return _client;
        }

        _client ??= CreateDefaultClient();
        return _client;
    }

    protected static CancellationToken Token => TestContext.Current.CancellationToken;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("CI");
        builder.ConfigureTestServices(ConfigureServices);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        foreach (var service in _serviceDescriptors)
        {
            services.Replace(service);
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = TestAuthHandler.AuthScheme;
            options.DefaultChallengeScheme = TestAuthHandler.AuthScheme;
        }).AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthScheme, _ => { });
    }

    protected void ReplaceService<T>(T instance) where T : class
    {
        _serviceDescriptors.Add(new ServiceDescriptor(typeof(T), instance));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client?.Dispose();
        }

        base.Dispose(disposing);
    }
}
