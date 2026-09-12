namespace NobaRental.Backend.Domain.Exceptions;

public class RentalValidationException : Exception
{
    public RentalValidationException()
    {
    }

    public RentalValidationException(string message)
        : base(message)
    {
    }

    public RentalValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
