using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NobaRental.Backend.WebApi.Auth;
using NobaRental.Backend.WebApi.Settings;
using System.Security.Claims;

namespace NobaRental.Backend.WebApi.Startups;

internal static class AuthenticationStartup
{
    public static void ConfigureAuthentication(this IServiceCollection services, AuthSettings authSettings)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = authSettings.Authority;
            options.Audience = authSettings.Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(authSettings.Authority),
                ValidateAudience = !string.IsNullOrWhiteSpace(authSettings.Audience),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthConstants.Policies.RentalsPickup, p => p.RequireAssertion(ctx => HasPermission(ctx.User, AuthConstants.Permissions.RentalsPickup)))
            .AddPolicy(AuthConstants.Policies.RentalsReturn, p => p.RequireAssertion(ctx => HasPermission(ctx.User, AuthConstants.Permissions.RentalsReturn)))
            .AddPolicy(AuthConstants.Policies.RentalsRead, p => p.RequireAssertion(ctx => HasPermission(ctx.User, AuthConstants.Permissions.RentalsRead)))
            .AddPolicy(AuthConstants.Policies.FleetManage, p => p.RequireAssertion(ctx => HasPermission(ctx.User, AuthConstants.Permissions.FleetManage)));
    }

    private static bool HasPermission(ClaimsPrincipal user, string permission) =>
        user.HasClaim(AuthConstants.PermissionsClaimType, permission) ||
        user.FindAll(AuthConstants.ScopeClaimType)
            .Any(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(permission, StringComparer.Ordinal));
}
