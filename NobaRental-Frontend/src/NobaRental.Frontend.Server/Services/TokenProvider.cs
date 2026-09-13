using Microsoft.Extensions.Caching.Hybrid;
using NobaRental.Frontend.Server.Settings;
using System.Text.Json.Serialization;

namespace NobaRental.Frontend.Server.Services;

public class TokenProvider(
    HttpClient client,
    HybridCache hybridCache,
    AppSettings settings,
    ILogger<TokenProvider> logger) : ITokenProvider
{
    private const string CacheKey = "auth:m2m:nobarental-backendapi";
    private static readonly TimeSpan ExpiryThreshold = TimeSpan.FromMinutes(5);

    internal sealed record Auth0TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string? TokenType);

    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        var auth0 = settings.Auth0;
        if (string.IsNullOrWhiteSpace(auth0.ClientId) || string.IsNullOrWhiteSpace(auth0.ClientSecret))
        {
            logger.LogWarning("Auth0 ClientId or ClientSecret is not configured.");
            return null;
        }

        return await hybridCache.GetOrCreateAsync(
            CacheKey,
            async ct => await RequestNewTokenAsync(auth0, ct),
            cancellationToken: cancellationToken);
    }

    private async ValueTask<string> RequestNewTokenAsync(Auth0Config auth0, CancellationToken ct)
    {
        var requestParams = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["client_id"] = auth0.ClientId,
            ["client_secret"] = auth0.ClientSecret,
            ["audience"] = auth0.Audience,
            ["grant_type"] = "client_credentials",
            ["scope"] = "rentals:pickup rentals:return rentals:read fleet:manage",
        };

        using var content = new FormUrlEncodedContent(requestParams);
        using var response = await client.PostAsync("oauth/token", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Failed to obtain Auth0 M2M token. StatusCode: {StatusCode}, Error: {Error}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var tokenData = await response.Content.ReadFromJsonAsync<Auth0TokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty token response received from Auth0.");

        var lifetime = TimeSpan.FromSeconds(tokenData.ExpiresIn);
        var cacheDuration = lifetime > ExpiryThreshold ? lifetime - ExpiryThreshold : TimeSpan.FromMinutes(1);

        await hybridCache.SetAsync(
            CacheKey,
            tokenData.AccessToken,
            new HybridCacheEntryOptions { Expiration = cacheDuration },
            cancellationToken: ct);

        return tokenData.AccessToken;
    }
}
