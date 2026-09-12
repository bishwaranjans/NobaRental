using Microsoft.AspNetCore.Mvc;
using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Values;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;
using NobaRental.Backend.WebApi.Mapping;

namespace NobaRental.Backend.WebApi.Controllers;

[ApiController]
[Route("api/v1/cars")]
public class CarsController(ICarFleetApi carFleetApi) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CarResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterCar([FromBody] RegisterCarRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await carFleetApi.RegisterCar(
                registrationNumber: request.RegistrationNumber,
                category: (CarCategory)request.Category,
                initialMeterReadingKm: request.InitialMeterReadingKm,
                stationCode: request.StationCode,
                cancellationToken: cancellationToken);

            return CreatedAtAction(nameof(GetByRegistrationNumber), new { registrationNumber = result.RegistrationNumber }, result.MapToResponse());
        }
        catch (InvalidRentalOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Car Registration Conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet]
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
        var domainStatus = status.HasValue ? (CarStatus?)status.Value : null;
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCar(string registrationNumber, CancellationToken cancellationToken)
    {
        try
        {
            await carFleetApi.DeleteCar(registrationNumber, cancellationToken);
            return NoContent();
        }
        catch (InvalidRentalOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Decommission Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet("available")]
    [ProducesResponseType<IReadOnlyCollection<CarResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] string? stationCode = null,
        [FromQuery] CarCategoryDto? category = null,
        CancellationToken cancellationToken = default)
    {
        var domainCategory = category.HasValue ? (CarCategory?)category.Value : null;
        var cars = await carFleetApi.GetAvailableCars(stationCode, domainCategory, cancellationToken);
        return Ok(cars.MapToResponse());
    }

    [HttpGet("{registrationNumber}")]
    [ProducesResponseType<CarResponse>(StatusCodes.Status200OK)]
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

        return Ok(car.MapToResponse());
    }
}
