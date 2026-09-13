using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.WebApi.Auth;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;
using NobaRental.Backend.WebApi.Mapping;

namespace NobaRental.Backend.WebApi.Controllers;

[ApiController]
[Route("api/v1/rentals")]
public class RentalBookingsController(IRentalBookingApi rentalBookingApi) : ControllerBase
{
    [HttpPost("pickup")]
    [Authorize(Policy = AuthConstants.Policies.RentalsPickup)]
    [ProducesResponseType<RentalBookingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterPickup([FromBody] RegisterPickupRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await rentalBookingApi.RegisterPickup(
                registrationNumber: request.RegistrationNumber,
                customerSsn: request.CustomerSsn,
                category: request.Category.MapToDomain(),
                pickupStationCode: request.PickupStationCode,
                pickupDateTime: request.PickupDateTime,
                pickupMeterReadingKm: request.PickupMeterReadingKm,
                baseDayRental: request.BaseDayRental,
                baseKmPrice: request.BaseKmPrice,
                cancellationToken: cancellationToken);

            return CreatedAtAction(nameof(GetByBookingNumber), new { bookingNumber = result.BookingNumber }, result.MapToResponse());
        }
        catch (InvalidRentalOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Invalid Rental Operation",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (RentalValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Rental Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("return")]
    [Authorize(Policy = AuthConstants.Policies.RentalsReturn)]
    [ProducesResponseType<RentalBookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterReturn([FromBody] RegisterReturnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await rentalBookingApi.RegisterReturn(
                bookingNumber: request.BookingNumber,
                returnStationCode: request.ReturnStationCode,
                returnDateTime: request.ReturnDateTime,
                returnMeterReadingKm: request.ReturnMeterReadingKm,
                rowVersion: request.RowVersion,
                cancellationToken: cancellationToken);

            return Ok(result.MapToResponse());
        }
        catch (BookingNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Booking Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (RentalConcurrencyException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Concurrency Conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (RentalValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Rental Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidRentalOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Invalid Rental Operation",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpGet("{bookingNumber:long}")]
    [Authorize(Policy = AuthConstants.Policies.RentalsRead)]
    [ProducesResponseType<RentalBookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByBookingNumber(long bookingNumber, CancellationToken cancellationToken)
    {
        var booking = await rentalBookingApi.GetBookingByNumber(bookingNumber, cancellationToken);
        if (booking is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Booking Not Found",
                Detail = $"Booking with number '{bookingNumber}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(booking.MapToResponse());
    }

    [HttpGet]
    [Authorize(Policy = AuthConstants.Policies.RentalsRead)]
    [ProducesResponseType<PagedResultResponse<RentalBookingResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultResponse<RentalBookingResponse>>> GetBookings(
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? stationCode = null,
        [FromQuery] RentalStatusDto? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool? sortDescending = null,
        CancellationToken cancellationToken = default)
    {
        var domainStatus = status.MapToDomain();
        var paged = await rentalBookingApi.GetBookings(
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

    [HttpGet("active")]
    [Authorize(Policy = AuthConstants.Policies.RentalsRead)]
    [ProducesResponseType<IReadOnlyCollection<RentalBookingResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var bookings = await rentalBookingApi.GetActiveBookings(cancellationToken);
        return Ok(bookings.MapToResponse());
    }

    [HttpPost("estimate-price")]
    [Authorize(Policy = AuthConstants.Policies.RentalsRead)]
    [ProducesResponseType<EstimatePriceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EstimatePrice([FromBody] EstimatePriceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var estimate = await rentalBookingApi.EstimatePrice(
                bookingNumber: request.BookingNumber,
                returnDateTime: request.ReturnDateTime,
                returnMeterReadingKm: request.ReturnMeterReadingKm,
                cancellationToken: cancellationToken);

            return Ok(new EstimatePriceResponse(
                BookingNumber: estimate.BookingNumber,
                CalculatedDays: estimate.CalculatedDays,
                CalculatedKm: estimate.CalculatedKm,
                EstimatedPrice: estimate.EstimatedPrice,
                Currency: estimate.Currency));
        }
        catch (BookingNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Booking Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
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
}
