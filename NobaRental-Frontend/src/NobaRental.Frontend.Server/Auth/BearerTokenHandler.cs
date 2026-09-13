using NobaRental.Frontend.Server.Services;
using System.Net.Http.Headers;

namespace NobaRental.Frontend.Server.Auth;

public class BearerTokenHandler(ITokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await tokenProvider.GetAccessToken(cancellationToken);
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
