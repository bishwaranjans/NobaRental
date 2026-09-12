namespace NobaRental.Backend.Domain.Exceptions;

public class BookingNotFoundException : Exception
{
    public long? BookingNumber { get; }

    public BookingNotFoundException()
    {
    }

    public BookingNotFoundException(long bookingNumber)
        : base($"Booking with number '{bookingNumber}' was not found.")
    {
        BookingNumber = bookingNumber;
    }

    public BookingNotFoundException(string message)
        : base(message)
    {
    }

    public BookingNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
