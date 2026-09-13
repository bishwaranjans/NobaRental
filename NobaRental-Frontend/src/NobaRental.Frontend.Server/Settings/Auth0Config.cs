namespace NobaRental.Frontend.Server.Settings;

public sealed record Auth0Config
{
    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}
