namespace NobaRental.Backend.Domain.Exceptions;

public class CategoryInUseException : Exception
{
    public string? Code { get; }
    public int Count { get; }

    public CategoryInUseException() { }

    public CategoryInUseException(string message)
        : base(message) { }

    public CategoryInUseException(string code, int count)
        : base($"Category '{code}' cannot be deleted because it is associated with {count} car(s) or booking(s).")
    {
        Code = code;
        Count = count;
    }

    public CategoryInUseException(string message, Exception innerException)
        : base(message, innerException) { }
}
