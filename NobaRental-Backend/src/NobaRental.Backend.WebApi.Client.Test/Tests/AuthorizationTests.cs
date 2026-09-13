using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace NobaRental.Backend.WebApi.Client.Test.Tests;

public sealed class AuthorizationTests : WebApplicationFactory<Program>
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/rentals/active", Token);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedCarsEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/cars/available", Token);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedStationsEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/stations", Token);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StatusEndpoint_WithoutToken_ReturnsOk()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/status", Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
