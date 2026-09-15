namespace NobaRental.Backend.Domain.Exceptions;

public class CategoryNotFoundException : Exception
{
    public string? Code { get; }

    public CategoryNotFoundException() { }

    public CategoryNotFoundException(string message)
        : base(message) { }

    public CategoryNotFoundException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public CategoryNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}
