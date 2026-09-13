using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.Domain.Values;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Test.Helpers;
using NSubstitute;
using RestEase;
using DtoCarCategory = NobaRental.Backend.WebApi.Client.Models.Values.CarCategoryDto;
using DtoCarStatus = NobaRental.Backend.WebApi.Client.Models.Values.CarStatusDto;

namespace NobaRental.Backend.WebApi.Client.Test.Tests;

public sealed class CarApiClientTests : TestWebHost
{
    private ICarApiClient Client => RestClient.For<ICarApiClient>(GetClient());

    [Fact]
    public async Task RegisterCar_Success()
    {
        // Arrange
        var api = Substitute.For<ICarFleetApi>();
        var domainCar = new Car("EV12345", CarCategory.SmallCar, 1500, CarStatus.Available, "OSL", BaseDayRental: 500m, BaseKmPrice: 0m);

        api.RegisterCar("EV12345", CarCategory.SmallCar, 1500, "OSL", 500m, 0m, Arg.Any<CancellationToken>())
           .Returns(domainCar);

        ReplaceService(api);

        var request = new RegisterCarRequest("EV12345", DtoCarCategory.SmallCar, 1500, "OSL", 500m, 0m);

        // Act
        using var result = await Client.RegisterCar(request, Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        var content = result.GetContent();
        Assert.NotNull(content);
        Assert.Equal("EV12345", content.RegistrationNumber);
        Assert.Equal(DtoCarCategory.SmallCar, content.Category);
        Assert.Equal(1500, content.CurrentMeterReadingKm);
        Assert.Equal(DtoCarStatus.Available, content.Status);
        Assert.Equal("OSL", content.CurrentStationCode);
        Assert.Equal(500m, content.BaseDayRental);
        Assert.Equal(0m, content.BaseKmPrice);
    }

    [Fact]
    public async Task GetCars_Success()
    {
        // Arrange
        var api = Substitute.For<ICarFleetApi>();
        var domainCars = new List<Car>
        {
            new("EV12345", CarCategory.SmallCar, 1000, CarStatus.Available, "OSL"),
            new("BT20001", CarCategory.Combi, 5000, CarStatus.Rented, "BGO"),
        };
        var pagedResult = new PagedResult<Car>(domainCars, 2, 1, 10);

        api.GetCars(1, 10, null, null, null, null, false, Arg.Any<CancellationToken>())
           .Returns(pagedResult);
        ReplaceService(api);

        // Act
        using var result = await Client.GetCars(1, 10, cancellationToken: Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        var content = result.GetContent();
        Assert.NotNull(content);
        Assert.Equal(2, content.TotalCount);
        Assert.Equal(2, content.Items.Count);
    }

    [Fact]
    public async Task GetAvailableCars_Success()
    {
        // Arrange
        var api = Substitute.For<ICarFleetApi>();
        var domainCars = new List<Car>
        {
            new("EV12345", CarCategory.SmallCar, 1000, CarStatus.Available, "OSL"),
        };

        api.GetAvailableCars("OSL", CarCategory.SmallCar, Arg.Any<CancellationToken>()).Returns(domainCars);
        ReplaceService(api);

        // Act
        using var result = await Client.GetAvailableCars("OSL", DtoCarCategory.SmallCar, Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        var content = result.GetContent();
        Assert.NotNull(content);
        var item = Assert.Single(content);
        Assert.Equal("EV12345", item.RegistrationNumber);
        Assert.Equal(DtoCarStatus.Available, item.Status);
        Assert.Equal("OSL", item.CurrentStationCode);
    }

    [Fact]
    public async Task GetCarByRegistrationNumber_Success()
    {
        // Arrange
        var api = Substitute.For<ICarFleetApi>();
        var domainCar = new Car("BT20001", CarCategory.Combi, 5000, CarStatus.Available, "SVG");

        api.GetCarByRegistrationNumber("BT20001", Arg.Any<CancellationToken>()).Returns(domainCar);
        ReplaceService(api);

        // Act
        using var result = await Client.GetCarByRegistrationNumber("BT20001", Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        var content = result.GetContent();
        Assert.NotNull(content);
        Assert.Equal("BT20001", content.RegistrationNumber);
        Assert.Equal(DtoCarCategory.Combi, content.Category);
        Assert.Equal("SVG", content.CurrentStationCode);
    }

    [Fact]
    public async Task GetCarByRegistrationNumber_NotFound()
    {
        // Arrange
        var api = Substitute.For<ICarFleetApi>();
        api.GetCarByRegistrationNumber("UNKNOWN", Arg.Any<CancellationToken>()).Returns((Car?)null);
        ReplaceService(api);

        // Act
        using var result = await Client.GetCarByRegistrationNumber("UNKNOWN", Token);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.ResponseMessage.StatusCode);
    }

    [Fact]
    public async Task DeleteCar_Success()
    {
        // Arrange
        var api = Substitute.For<ICarFleetApi>();
        api.DeleteCar("EV12345", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        ReplaceService(api);

        // Act
        using var result = await Client.DeleteCar("EV12345", Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetCarByRegistrationNumber_WithIfNoneMatch_Returns304NotModified()
    {
        // Arrange
        var api = Substitute.For<ICarFleetApi>();
        byte[] rowVersion = [11, 22, 33, 44];
        var domainCar = new Car("EV12345", CarCategory.SmallCar, 1500, CarStatus.Available, "OSL", RowVersion: rowVersion);
        api.GetCarByRegistrationNumber("EV12345", Arg.Any<CancellationToken>()).Returns(domainCar);
        ReplaceService(api);

        using var client = GetClient();
        client.DefaultRequestHeaders.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{Convert.ToBase64String(rowVersion)}\""));

        // Act
        using var response = await client.GetAsync("/api/v1/cars/EV12345", Token);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotModified, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCarTariff_Success()
    {
        // Arrange
        var api = Substitute.For<ICarFleetApi>();
        byte[] rowVersion = [1, 2, 3, 4];
        var domainCar = new Car("EV12345", CarCategory.SmallCar, 1500, CarStatus.Available, "OSL", BaseDayRental: 650m, BaseKmPrice: 0m, RowVersion: rowVersion);

        api.UpdateCarTariff("EV12345", 650m, 0m, Arg.Any<byte[]?>(), Arg.Any<CancellationToken>())
           .Returns(domainCar);

        ReplaceService(api);

        var request = new UpdateCarTariffRequest(650m, 0m, rowVersion);
        var ifMatch = $"\"{Convert.ToBase64String(rowVersion)}\"";

        // Act
        using var result = await Client.UpdateCarTariff("EV12345", request, ifMatch, Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        Assert.NotNull(result.ResponseMessage.Headers.ETag);
        var content = result.GetContent();
        Assert.NotNull(content);
        Assert.Equal(650m, content.BaseDayRental);
        Assert.Equal(0m, content.BaseKmPrice);
    }

    [Fact]
    public async Task UpdateCarTariff_WithMismatchedIfMatch_Returns412PreconditionFailed()
    {
        // Arrange
        var api = Substitute.For<ICarFleetApi>();
        api.UpdateCarTariff("EV12345", 650m, 0m, Arg.Any<byte[]?>(), Arg.Any<CancellationToken>())
           .Returns<Car>(_ => throw new RentalConcurrencyException("Concurrency conflict"));
        ReplaceService(api);

        byte[] requestRowVersion = [1, 2, 3, 4];
        byte[] headerRowVersion = [9, 9, 9, 9];
        var request = new UpdateCarTariffRequest(650m, 0m, requestRowVersion);
        var ifMatch = $"\"{Convert.ToBase64String(headerRowVersion)}\"";

        // Act
        using var result = await Client.UpdateCarTariff("EV12345", request, ifMatch, Token);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.PreconditionFailed, result.ResponseMessage.StatusCode);
    }
}
