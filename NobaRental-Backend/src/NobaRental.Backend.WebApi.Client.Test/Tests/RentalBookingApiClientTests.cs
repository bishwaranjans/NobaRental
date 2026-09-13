using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.Domain.Values;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Test.Helpers;
using NSubstitute;
using RestEase;
using DtoCarCategory = NobaRental.Backend.WebApi.Client.Models.Values.CarCategoryDto;
using DtoRentalStatus = NobaRental.Backend.WebApi.Client.Models.Values.RentalStatusDto;

namespace NobaRental.Backend.WebApi.Client.Test.Tests;

public sealed class RentalBookingApiClientTests : TestWebHost
{
    private IRentalBookingApiClient Client => RestClient.For<IRentalBookingApiClient>(GetClient());

    private static RentalBooking CreateDomainBooking(
        long bookingNumber,
        string regNumber,
        string ssn,
        CarCategory category,
        string pickupStation,
        DateTimeOffset pickupTime,
        long pickupKm,
        decimal baseDay = 500m,
        decimal baseKm = 2m) =>
        new(
            BookingNumber: bookingNumber,
            RegistrationNumber: regNumber,
            CustomerSsn: ssn,
            Category: category,
            PickupStationCode: pickupStation,
            PickupDateTime: pickupTime,
            PickupMeterReadingKm: pickupKm,
            ReturnStationCode: null,
            ReturnDateTime: null,
            ReturnMeterReadingKm: null,
            BaseDayRental: baseDay,
            BaseKmPrice: baseKm,
            CalculatedDays: null,
            CalculatedKm: null,
            TotalPrice: null,
            Currency: "NOK",
            Status: RentalStatus.Active);

    [Fact]
    public async Task RegisterPickup_Success()
    {
        // Arrange
        var api = Substitute.For<IRentalBookingApi>();
        var pickupTime = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var domainBooking = CreateDomainBooking(101L, "EV12345", "12345678901", CarCategory.SmallCar, "OSL", pickupTime, 10000);

        api.RegisterPickup("EV12345", "12345678901", CarCategory.SmallCar, "OSL", Arg.Any<DateTimeOffset>(), 10000, 500m, 2m, Arg.Any<CancellationToken>())
           .Returns(domainBooking);

        ReplaceService(api);

        var request = new RegisterPickupRequest(
            RegistrationNumber: "EV12345",
            CustomerSsn: "12345678901",
            Category: DtoCarCategory.SmallCar,
            PickupStationCode: "OSL",
            PickupDateTime: pickupTime,
            PickupMeterReadingKm: 10000,
            BaseDayRental: 500m,
            BaseKmPrice: 2m);

        // Act
        using var result = await Client.RegisterPickup(request, Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        var content = result.GetContent();
        Assert.NotNull(content);
        Assert.Equal(101L, content.BookingNumber);
        Assert.Equal("EV12345", content.RegistrationNumber);
        Assert.Equal("12345678901", content.CustomerSsn);
        Assert.Equal(DtoCarCategory.SmallCar, content.Category);
        Assert.Equal(DtoRentalStatus.Active, content.Status);
        Assert.Equal("OSL", content.PickupStationCode);
        Assert.Equal("NOK", content.Currency);
    }

    [Fact]
    public async Task RegisterReturn_Success()
    {
        // Arrange
        var api = Substitute.For<IRentalBookingApi>();
        var pickupTime = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var returnTime = pickupTime.AddDays(2);

        var domainBooking = new RentalBooking(
            BookingNumber: 101L,
            RegistrationNumber: "EV12345",
            CustomerSsn: "12345678901",
            Category: CarCategory.SmallCar,
            PickupStationCode: "OSL",
            PickupDateTime: pickupTime,
            PickupMeterReadingKm: 10000,
            ReturnStationCode: "BGO",
            ReturnDateTime: returnTime,
            ReturnMeterReadingKm: 10350,
            BaseDayRental: 500m,
            BaseKmPrice: 2m,
            CalculatedDays: 2,
            CalculatedKm: 350,
            TotalPrice: 1000m,
            Currency: "NOK",
            Status: RentalStatus.Completed);

        api.RegisterReturn(101L, "BGO", Arg.Any<DateTimeOffset>(), 10350, Arg.Any<byte[]?>(), Arg.Any<CancellationToken>())
           .Returns(domainBooking);

        ReplaceService(api);

        var request = new RegisterReturnRequest(
            BookingNumber: 101L,
            ReturnStationCode: "BGO",
            ReturnDateTime: returnTime,
            ReturnMeterReadingKm: 10350);

        // Act
        using var result = await Client.RegisterReturn(request, Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        var content = result.GetContent();
        Assert.NotNull(content);
        Assert.Equal(101L, content.BookingNumber);
        Assert.Equal(DtoRentalStatus.Completed, content.Status);
        Assert.Equal("BGO", content.ReturnStationCode);
        Assert.Equal(1000m, content.TotalPrice);
        Assert.Equal(2, content.CalculatedDays);
        Assert.Equal(350, content.CalculatedKm);
    }

    [Fact]
    public async Task GetBookingByNumber_Success()
    {
        // Arrange
        var api = Substitute.For<IRentalBookingApi>();
        var domainBooking = new RentalBooking(
            BookingNumber: 202L,
            RegistrationNumber: "BT99999",
            CustomerSsn: "98765432100",
            Category: CarCategory.Combi,
            PickupStationCode: "OSL",
            PickupDateTime: new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero),
            PickupMeterReadingKm: 50000,
            ReturnStationCode: null,
            ReturnDateTime: null,
            ReturnMeterReadingKm: null,
            BaseDayRental: 600m,
            BaseKmPrice: 3m,
            CalculatedDays: null,
            CalculatedKm: null,
            TotalPrice: null,
            Currency: "NOK",
            Status: RentalStatus.Active);

        api.GetBookingByNumber(202L, Arg.Any<CancellationToken>()).Returns(domainBooking);
        ReplaceService(api);

        // Act
        using var result = await Client.GetBookingByNumber(202L, Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        var content = result.GetContent();
        Assert.NotNull(content);
        Assert.Equal(202L, content.BookingNumber);
        Assert.Equal(DtoCarCategory.Combi, content.Category);
        Assert.Equal("OSL", content.PickupStationCode);
    }

    [Fact]
    public async Task GetBookings_Success()
    {
        // Arrange
        var api = Substitute.For<IRentalBookingApi>();
        var domainBooking = new RentalBooking(
            BookingNumber: 303L,
            RegistrationNumber: "BT11111",
            CustomerSsn: "11111111111",
            Category: CarCategory.Truck,
            PickupStationCode: "TRD",
            PickupDateTime: new DateTimeOffset(2026, 9, 11, 9, 0, 0, TimeSpan.Zero),
            PickupMeterReadingKm: 80000,
            ReturnStationCode: null,
            ReturnDateTime: null,
            ReturnMeterReadingKm: null,
            BaseDayRental: 1000m,
            BaseKmPrice: 5m,
            CalculatedDays: null,
            CalculatedKm: null,
            TotalPrice: null,
            Currency: "NOK",
            Status: RentalStatus.Active);

        var pagedResult = new PagedResult<RentalBooking>([domainBooking], 1, 1, 10);
        api.GetBookings(1, 10, null, null, null, null, false, Arg.Any<CancellationToken>())
           .Returns(pagedResult);
        ReplaceService(api);

        // Act
        using var result = await Client.GetBookings(1, 10, cancellationToken: Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        var content = result.GetContent();
        Assert.NotNull(content);
        Assert.Equal(1, content.TotalCount);
        var item = Assert.Single(content.Items);
        Assert.Equal(303L, item.BookingNumber);
        Assert.Equal(DtoCarCategory.Truck, item.Category);
    }

    [Fact]
    public async Task GetActiveBookings_Success()
    {
        // Arrange
        var api = Substitute.For<IRentalBookingApi>();
        var domainBooking = new RentalBooking(
            BookingNumber: 404L,
            RegistrationNumber: "BT22222",
            CustomerSsn: "22222222222",
            Category: CarCategory.SmallCar,
            PickupStationCode: "SVG",
            PickupDateTime: new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero),
            PickupMeterReadingKm: 20000,
            ReturnStationCode: null,
            ReturnDateTime: null,
            ReturnMeterReadingKm: null,
            BaseDayRental: 450m,
            BaseKmPrice: 1.5m,
            CalculatedDays: null,
            CalculatedKm: null,
            TotalPrice: null,
            Currency: "NOK",
            Status: RentalStatus.Active);

        api.GetActiveBookings(Arg.Any<CancellationToken>()).Returns([domainBooking]);
        ReplaceService(api);

        // Act
        using var result = await Client.GetActiveBookings(Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        var content = result.GetContent();
        Assert.NotNull(content);
        var item = Assert.Single(content);
        Assert.Equal(404L, item.BookingNumber);
        Assert.Equal(DtoRentalStatus.Active, item.Status);
    }

    [Fact]
    public async Task EstimatePrice_Success()
    {
        // Arrange
        var api = Substitute.For<IRentalBookingApi>();
        var returnTime = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var estimate = new RentalPriceEstimate(
            BookingNumber: 101L,
            CalculatedDays: 2,
            CalculatedKm: 150,
            EstimatedPrice: 1650m,
            Currency: "NOK");

        api.EstimatePrice(101L, returnTime, 10150, Arg.Any<CancellationToken>())
           .Returns(estimate);
        ReplaceService(api);

        var request = new EstimatePriceRequest(101L, returnTime, 10150);

        // Act
        using var result = await Client.EstimatePrice(request, Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
        var content = result.GetContent();
        Assert.NotNull(content);
        Assert.Equal(101L, content.BookingNumber);
        Assert.Equal(2, content.CalculatedDays);
        Assert.Equal(150, content.CalculatedKm);
        Assert.Equal(1650m, content.EstimatedPrice);
        Assert.Equal("NOK", content.Currency);
    }

    [Fact]
    public async Task Status_ReturnsOk()
    {
        // Act
        using var result = await Client.Status(Token);

        // Assert
        Assert.True(result.ResponseMessage.IsSuccessStatusCode);
    }
}
