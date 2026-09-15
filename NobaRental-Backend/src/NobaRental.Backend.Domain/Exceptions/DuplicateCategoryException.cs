namespace NobaRental.Backend.Domain.Exceptions;

public class DuplicateCategoryException : Exception
{
    public string? Code { get; }

    public DuplicateCategoryException() { }

    public DuplicateCategoryException(string message)
        : base(message) { }

    public DuplicateCategoryException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public DuplicateCategoryException(string message, Exception innerException)
        : base(message, innerException) { }
}
