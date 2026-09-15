using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.WebApi.Auth;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;
using NobaRental.Backend.WebApi.Mapping;
using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.WebApi.Helpers;

namespace NobaRental.Backend.WebApi.Controllers;

[ApiController]
[Route("api/v1/cars")]
public class CarsController(ICarFleetApi carFleetApi) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthConstants.Policies.FleetManage)]
    [ProducesResponseType<CarResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterCar([FromBody] RegisterCarRequest request, CancellationToken cancellationToken)
    {
        var result = await carFleetApi.RegisterCar(
            registrationNumber: request.RegistrationNumber,
            categoryCode: request.CategoryCode,
            initialMeterReadingKm: request.InitialMeterReadingKm,
            stationCode: request.StationCode,
            baseDayRental: request.BaseDayRental,
            baseKmPrice: request.BaseKmPrice,
            cancellationToken: cancellationToken);

        ETagHelper.SetETag(Response, result.RowVersion);
        return CreatedAtAction(nameof(GetByRegistrationNumber), new { registrationNumber = result.RegistrationNumber }, result.MapToResponse());
    }

    [HttpPut("{registrationNumber}/tariff")]
    [Authorize(Policy = AuthConstants.Policies.FleetManage)]
    [ProducesResponseType<CarResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> UpdateTariff(
        string registrationNumber,
        [FromBody] UpdateCarTariffRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var rowVersion = ResolveRowVersion(ifMatch, request.RowVersion, out var hasIfMatch);

        Car result;
        try
        {
            result = await carFleetApi.UpdateCarTariff(
                registrationNumber: registrationNumber,
                baseDayRental: request.BaseDayRental,
                baseKmPrice: request.BaseKmPrice,
                rowVersion: rowVersion,
                cancellationToken: cancellationToken);
        }
        catch (RentalConcurrencyException) when (hasIfMatch)
        {
            throw new PreconditionFailedException($"Car '{registrationNumber}' was modified by another operation.");
        }

        ETagHelper.SetETag(Response, result.RowVersion);
        return Ok(result.MapToResponse());
    }

    private static byte[]? ResolveRowVersion(string? ifMatch, byte[]? bodyRowVersion, out bool hasIfMatch)
    {
        hasIfMatch = !string.IsNullOrWhiteSpace(ifMatch);
        if (!hasIfMatch)
        {
            return bodyRowVersion;
        }

        if (!ETagHelper.TryParseETag(ifMatch, out var parsed))
        {
            throw new PreconditionFailedException("The provided If-Match header is invalid.");
        }

        return parsed;
    }

    [HttpGet]
    [Authorize(Policy = AuthConstants.Policies.RentalsRead)]
    [ProducesResponseType<PagedResultResponse<CarResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultResponse<CarResponse>>> GetCars(
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? stationCode = null,
        [FromQuery] CarStatusDto? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool? sortDescending = null,
        CancellationToken cancellationToken = default)
    {
        var domainStatus = status.MapToDomain();
        var paged = await carFleetApi.GetCars(
            pageNumber ?? 1,
            pageSize ?? 10,
            searchTerm,
            stationCode,
            domainStatus,
            sortBy,
            sortDescending ?? false,
            cancellationToken);

        return Ok(paged.MapToPagedResponse());
    }

    [HttpDelete("{registrationNumber}")]
    [Authorize(Policy = AuthConstants.Policies.FleetManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCar(string registrationNumber, CancellationToken cancellationToken)
    {
        await carFleetApi.DeleteCar(registrationNumber, cancellationToken);
        return NoContent();
    }

    [HttpGet("available")]
    [Authorize(Policy = AuthConstants.Policies.RentalsRead)]
    [ProducesResponseType<IReadOnlyCollection<CarResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] string? stationCode = null,
        [FromQuery] string? categoryCode = null,
        CancellationToken cancellationToken = default)
    {
        var cars = await carFleetApi.GetAvailableCars(stationCode, categoryCode, cancellationToken);
        return Ok(cars.MapToResponse());
    }

    [HttpGet("{registrationNumber}")]
    [Authorize(Policy = AuthConstants.Policies.RentalsRead)]
    [ProducesResponseType<CarResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByRegistrationNumber(string registrationNumber, CancellationToken cancellationToken)
    {
        var car = await carFleetApi.GetCarByRegistrationNumber(registrationNumber, cancellationToken);
        if (car is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Car Not Found",
                Detail = $"Car with registration number '{registrationNumber}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (ETagHelper.IsIfNoneMatch(Request, car.RowVersion))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        ETagHelper.SetETag(Response, car.RowVersion);
        return Ok(car.MapToResponse());
    }
}
