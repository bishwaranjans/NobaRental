namespace NobaRental.Backend.Domain.Exceptions;

public class PreconditionFailedException : Exception
{
    public PreconditionFailedException()
        : base("The resource condition specified in the request header did not match the current state.")
    {
    }

    public PreconditionFailedException(string message)
        : base(message)
    {
    }

    public PreconditionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
