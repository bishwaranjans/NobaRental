using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NobaRental.Backend.WebApi.Auth;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace NobaRental.Backend.WebApi.Client.Test.Helpers;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthScheme = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Claim[] claims =
        [
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.NameIdentifier, "test-client-id"),
            new(AuthConstants.PermissionsClaimType, AuthConstants.Permissions.RentalsPickup),
            new(AuthConstants.PermissionsClaimType, AuthConstants.Permissions.RentalsReturn),
            new(AuthConstants.PermissionsClaimType, AuthConstants.Permissions.RentalsRead),
            new(AuthConstants.PermissionsClaimType, AuthConstants.Permissions.FleetManage),
        ];

        var identity = new ClaimsIdentity(claims, AuthScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
