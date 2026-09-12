using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Test.Helpers;
using NSubstitute;
using RestEase;

namespace NobaRental.Backend.WebApi.Client.Test.Tests;

public sealed class StationApiClientTests : TestWebHost
{
    private IStationApiClient Client => RestClient.For<IStationApiClient>(GetClient());

    [Fact]
    public async Task GetAllStationsAsync_Success()
    {
        // Arrange
        var api = Substitute.For<IStationApi>();
        var domainStations = new List<Station>
        {
            new("OSL", "Oslo Airport Gardermoen", "Oslo", true),
            new("BGO", "Bergen Airport Flesland", "Bergen", true),
        };

        api.GetAllStations(false, Arg.Any<CancellationToken>()).Returns(domainStations);
        ReplaceService(api);

        // Act
        var result = await Client.GetAllStationsAsync(false, Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetStationByCodeAsync_Success()
    {
        // Arrange
        var api = Substitute.For<IStationApi>();
        var domainStation = new Station("TRD", "Trondheim Airport Værnes", "Trondheim", true);

        api.GetStationByCode("TRD", Arg.Any<CancellationToken>()).Returns(domainStation);
        ReplaceService(api);

        // Act
        var result = await Client.GetStationByCodeAsync("TRD", Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TRD", result.Code);
        Assert.Equal("Trondheim Airport Værnes", result.Name);
        Assert.Equal("Trondheim", result.City);
    }

    [Fact]
    public async Task CreateStationAsync_Success()
    {
        // Arrange
        var api = Substitute.For<IStationApi>();
        var domainStation = new Station("SVG", "Stavanger Airport Sola", "Stavanger", true);

        api.CreateStation("SVG", "Stavanger Airport Sola", "Stavanger", Arg.Any<CancellationToken>())
           .Returns(domainStation);
        ReplaceService(api);

        var request = new CreateStationRequest("SVG", "Stavanger Airport Sola", "Stavanger");

        // Act
        var result = await Client.CreateStationAsync(request, Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SVG", result.Code);
        Assert.Equal("Stavanger Airport Sola", result.Name);
    }

    [Fact]
    public async Task UpdateStationAsync_Success()
    {
        // Arrange
        var api = Substitute.For<IStationApi>();
        var domainStation = new Station("OSLO-C", "Oslo Central Station Renamed", "Oslo", true);

        api.UpdateStation("OSLO-C", "Oslo Central Station Renamed", "Oslo", true, Arg.Any<byte[]?>(), Arg.Any<CancellationToken>())
           .Returns(domainStation);
        ReplaceService(api);

        var request = new UpdateStationRequest("Oslo Central Station Renamed", "Oslo", true);

        // Act
        var result = await Client.UpdateStationAsync("OSLO-C", request, Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OSLO-C", result.Code);
        Assert.Equal("Oslo Central Station Renamed", result.Name);
    }

    [Fact]
    public async Task DeleteStationAsync_Success()
    {
        // Arrange
        var api = Substitute.For<IStationApi>();
        api.DeleteStation("OSL", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        ReplaceService(api);

        // Act
        await Client.DeleteStationAsync("OSL", Token);

        // Assert
        await api.Received(1).DeleteStation("OSL", Arg.Any<CancellationToken>());
    }
}
