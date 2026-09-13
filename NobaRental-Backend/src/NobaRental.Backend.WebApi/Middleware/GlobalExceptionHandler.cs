using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NobaRental.Backend.Domain.Exceptions;

namespace NobaRental.Backend.WebApi.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning("Request failed with status {StatusCode} ({Title}): {Message}", statusCode, title, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception ex) =>
        ex switch
        {
            BookingNotFoundException notFound => (StatusCodes.Status404NotFound, "Booking Not Found", notFound.Message),
            PreconditionRequiredException required => (StatusCodes.Status428PreconditionRequired, "Precondition Required", required.Message),
            PreconditionFailedException precondition => (StatusCodes.Status412PreconditionFailed, "Precondition Failed", precondition.Message),
            RentalConcurrencyException concurrency => (StatusCodes.Status409Conflict, "Concurrency Conflict", concurrency.Message),
            StationInUseException inUse => (StatusCodes.Status409Conflict, "Station In Use", inUse.Message),
            InvalidRentalOperationException invalidOp => (StatusCodes.Status409Conflict, "Invalid Rental Operation", invalidOp.Message),
            RentalValidationException validation => (StatusCodes.Status400BadRequest, "Rental Validation Error", validation.Message),
            ArgumentOutOfRangeException argOutOfRange => (StatusCodes.Status400BadRequest, "Validation Error", argOutOfRange.Message),
            ArgumentException arg => (StatusCodes.Status400BadRequest, "Validation Error", arg.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred. Please try again later.")
        };
}
