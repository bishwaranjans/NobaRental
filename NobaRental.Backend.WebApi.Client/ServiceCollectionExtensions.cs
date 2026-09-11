using Microsoft.Extensions.DependencyInjection;
using RestEase.HttpClientFactory;

namespace NobaRental.Backend.WebApi.Client;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddNobaRentalApiClients(Uri baseUri,
            Action<IHttpClientBuilder>? httpClientBuilderAction = null)
        {
            IHttpClientBuilder[] clients =
            [
                services.AddNobaRentalApiClient<IWeatherForecastClient>(baseUri)
            ];

            if (httpClientBuilderAction is not null)
            {
                foreach (var client in clients)
                {
                    httpClientBuilderAction(client);
                }
            }
        }

        private IHttpClientBuilder AddNobaRentalApiClient<T>(Uri baseUri)
            where T : class
        {
            var options = new AddRestEaseClientOptions<T>();
            return services.AddRestEaseClient(baseUri, options);
        }
    }
}