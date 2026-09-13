using NobaRental.Backend.Domain.Values;
using NobaRental.Backend.WebApi.Client.Models.Values;
using NobaRental.Backend.WebApi.Mapping;

namespace NobaRental.Backend.WebApi.Client.Test.Tests;

public sealed class WebApiEnumMappingTests
{
    [Theory]
    [InlineData(CarCategoryDto.SmallCar, CarCategory.SmallCar)]
    [InlineData(CarCategoryDto.Combi, CarCategory.Combi)]
    [InlineData(CarCategoryDto.Truck, CarCategory.Truck)]
    public void CarCategory_MapToDomain_MapsCorrectly(CarCategoryDto dto, CarCategory expectedDomain)
    {
        Assert.Equal(expectedDomain, dto.MapToDomain());
    }

    [Theory]
    [InlineData(CarCategory.SmallCar, CarCategoryDto.SmallCar)]
    [InlineData(CarCategory.Combi, CarCategoryDto.Combi)]
    [InlineData(CarCategory.Truck, CarCategoryDto.Truck)]
    public void CarCategory_MapToResponse_MapsCorrectly(CarCategory domain, CarCategoryDto expectedDto)
    {
        Assert.Equal(expectedDto, domain.MapToResponse());
    }

    [Fact]
    public void CarCategory_MapToDomain_Nullable_MapsCorrectly()
    {
        CarCategoryDto? nullDto = null;
        Assert.Null(nullDto.MapToDomain());

        CarCategoryDto? nonNullDto = CarCategoryDto.SmallCar;
        Assert.Equal(CarCategory.SmallCar, nonNullDto.MapToDomain());
    }

    [Fact]
    public void CarCategory_MapToDomain_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CarCategoryDto)999).MapToDomain());
    }

    [Fact]
    public void CarCategory_MapToResponse_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CarCategory)999).MapToResponse());
    }

    [Theory]
    [InlineData(CarStatusDto.Available, CarStatus.Available)]
    [InlineData(CarStatusDto.Rented, CarStatus.Rented)]
    [InlineData(CarStatusDto.Maintenance, CarStatus.Maintenance)]
    [InlineData(CarStatusDto.Decommissioned, CarStatus.Decommissioned)]
    public void CarStatus_MapToDomain_MapsCorrectly(CarStatusDto dto, CarStatus expectedDomain)
    {
        Assert.Equal(expectedDomain, dto.MapToDomain());
    }

    [Theory]
    [InlineData(CarStatus.Available, CarStatusDto.Available)]
    [InlineData(CarStatus.Rented, CarStatusDto.Rented)]
    [InlineData(CarStatus.Maintenance, CarStatusDto.Maintenance)]
    [InlineData(CarStatus.Decommissioned, CarStatusDto.Decommissioned)]
    public void CarStatus_MapToResponse_MapsCorrectly(CarStatus domain, CarStatusDto expectedDto)
    {
        Assert.Equal(expectedDto, domain.MapToResponse());
    }

    [Fact]
    public void CarStatus_MapToDomain_Nullable_MapsCorrectly()
    {
        CarStatusDto? nullDto = null;
        Assert.Null(nullDto.MapToDomain());

        CarStatusDto? nonNullDto = CarStatusDto.Available;
        Assert.Equal(CarStatus.Available, nonNullDto.MapToDomain());
    }

    [Fact]
    public void CarStatus_MapToDomain_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CarStatusDto)999).MapToDomain());
    }

    [Fact]
    public void CarStatus_MapToResponse_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CarStatus)999).MapToResponse());
    }

    [Theory]
    [InlineData(RentalStatusDto.Active, RentalStatus.Active)]
    [InlineData(RentalStatusDto.Completed, RentalStatus.Completed)]
    public void RentalStatus_MapToDomain_MapsCorrectly(RentalStatusDto dto, RentalStatus expectedDomain)
    {
        Assert.Equal(expectedDomain, dto.MapToDomain());
    }

    [Theory]
    [InlineData(RentalStatus.Active, RentalStatusDto.Active)]
    [InlineData(RentalStatus.Completed, RentalStatusDto.Completed)]
    public void RentalStatus_MapToResponse_MapsCorrectly(RentalStatus domain, RentalStatusDto expectedDto)
    {
        Assert.Equal(expectedDto, domain.MapToResponse());
    }

    [Fact]
    public void RentalStatus_MapToDomain_Nullable_MapsCorrectly()
    {
        RentalStatusDto? nullDto = null;
        Assert.Null(nullDto.MapToDomain());

        RentalStatusDto? nonNullDto = RentalStatusDto.Active;
        Assert.Equal(RentalStatus.Active, nonNullDto.MapToDomain());
    }

    [Fact]
    public void RentalStatus_MapToDomain_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((RentalStatusDto)999).MapToDomain());
    }

    [Fact]
    public void RentalStatus_MapToResponse_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((RentalStatus)999).MapToResponse());
    }
}
