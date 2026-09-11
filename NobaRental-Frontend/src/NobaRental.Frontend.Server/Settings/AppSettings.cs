namespace NobaRental.Frontend.Server.Settings;

public class AppSettings
{
    public NobaRentalBackendApiSettings NobaRentalBackendApi { get; init; } = new();
}