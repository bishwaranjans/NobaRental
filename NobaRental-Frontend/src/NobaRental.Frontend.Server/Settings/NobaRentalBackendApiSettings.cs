namespace NobaRental.Frontend.Server.Settings;

public class NobaRentalBackendApiSettings
{
    public Uri BaseUri { get; init; } = new("https+http://nobarental-backendapi");
}
