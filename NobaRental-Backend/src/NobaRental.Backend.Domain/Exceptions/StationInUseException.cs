namespace NobaRental.Backend.Domain.Exceptions;

public class StationInUseException : Exception
{
    public string? StationCode { get; }

    public StationInUseException()
    {
    }

    public StationInUseException(string message)
        : base(message)
    {
    }

    public StationInUseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public StationInUseException(string stationCode, string message)
        : base(message)
    {
        StationCode = stationCode;
    }
}
