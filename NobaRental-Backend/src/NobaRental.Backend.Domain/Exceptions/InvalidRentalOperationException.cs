namespace NobaRental.Backend.Domain.Exceptions;

public class InvalidRentalOperationException : Exception
{
    public InvalidRentalOperationException()
    {
    }

    public InvalidRentalOperationException(string message)
        : base(message)
    {
    }

    public InvalidRentalOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
