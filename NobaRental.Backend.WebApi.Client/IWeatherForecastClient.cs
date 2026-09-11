using NobaRental.Backend.WebApi.Client.Models;
using RestEase;

namespace NobaRental.Backend.WebApi.Client;

public interface IWeatherForecastClient
{
    [Get("weatherforecast")]
    [AllowAnyStatusCode]
    Task<Response<WeatherForecast[]>> GetWeatherForecast(CancellationToken cancellationToken = default);

    [Get("health")]
    [AllowAnyStatusCode]
    Task<Response<string>> Status(CancellationToken cancellationToken = default);
}
