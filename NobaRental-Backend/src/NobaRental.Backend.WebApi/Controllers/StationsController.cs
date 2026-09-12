using Microsoft.AspNetCore.Mvc;
using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Mapping;

namespace NobaRental.Backend.WebApi.Controllers;

[ApiController]
[Route("api/v1/stations")]
public class StationsController(IStationApi stationApi) : ControllerBase
{
    private readonly IStationApi _stationApi = stationApi;

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<StationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<StationResponse>>> GetAllStations(
        [FromQuery] bool? includeInactive = null,
        CancellationToken cancellationToken = default)
    {
        var stations = await _stationApi.GetAllStations(includeInactive ?? false, cancellationToken);
        return Ok(stations.MapToResponse());
    }

    [HttpGet("{code}")]
    [ProducesResponseType<StationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StationResponse>> GetStationByCode(
        string code,
        CancellationToken cancellationToken = default)
    {
        var station = await _stationApi.GetStationByCode(code, cancellationToken);
        if (station is null)
        {
            return NotFound();
        }

        return Ok(station.MapToResponse());
    }

    [HttpPost]
    [ProducesResponseType<StationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StationResponse>> CreateStation(
        [FromBody] CreateStationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var station = await _stationApi.CreateStation(request.Code, request.Name, request.City, cancellationToken);
            var response = station.MapToResponse();
            return CreatedAtAction(nameof(GetStationByCode), new { code = response.Code }, response);
        }
        catch (InvalidRentalOperationException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Operation", Detail = ex.Message });
        }
    }

    [HttpPut("{code}")]
    [ProducesResponseType<StationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StationResponse>> UpdateStation(
        string code,
        [FromBody] UpdateStationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _stationApi.UpdateStation(code, request.Name, request.City, request.IsActive, request.RowVersion, cancellationToken);
            return Ok(updated.MapToResponse());
        }
        catch (RentalConcurrencyException ex)
        {
            return Conflict(new ProblemDetails { Title = "Concurrency Conflict", Detail = ex.Message });
        }
        catch (InvalidRentalOperationException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = ex.Message });
        }
    }

    [HttpDelete("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteStation(
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _stationApi.DeleteStation(code, cancellationToken);
            return NoContent();
        }
        catch (StationInUseException ex)
        {
            return Conflict(new ProblemDetails { Title = "Station In Use", Detail = ex.Message });
        }
    }
}
