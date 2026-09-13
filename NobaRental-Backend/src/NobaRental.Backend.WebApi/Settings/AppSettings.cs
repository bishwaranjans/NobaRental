namespace NobaRental.Backend.WebApi.Settings;

public class AppSettings
{
    public required ConnectionStrings ConnectionStrings { get; init; }
    public AuthSettings Auth { get; init; } = new();
}