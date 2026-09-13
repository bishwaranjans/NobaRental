namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record UpdateCarTariffRequest(
    decimal BaseDayRental,
    decimal BaseKmPrice = 0m,
    byte[]? RowVersion = null);
