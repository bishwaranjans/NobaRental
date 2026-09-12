namespace NobaRental.Backend.Domain.Exceptions;

public class RentalConcurrencyException : Exception
{
    public RentalConcurrencyException()
        : base("The record has been modified by another operation. Please reload and try again.")
    {
    }

    public RentalConcurrencyException(string message)
        : base(message)
    {
    }

    public RentalConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
