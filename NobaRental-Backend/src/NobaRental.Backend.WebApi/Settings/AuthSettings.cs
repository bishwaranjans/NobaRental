namespace NobaRental.Backend.WebApi.Settings;

public sealed record AuthSettings
{
    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
}
