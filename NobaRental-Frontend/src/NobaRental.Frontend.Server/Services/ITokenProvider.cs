namespace NobaRental.Frontend.Server.Services;

public interface ITokenProvider
{
    Task<string?> GetAccessToken(CancellationToken cancellationToken = default);
}
