namespace NobaRental.Frontend.Server.Settings;

public class AppSettings
{
    public NobaRentalBackendApiSettings NobaRentalBackendApi { get; init; } = new();
    public Auth0Config Auth0 { get; init; } = new();
}