namespace NobaRental.Backend.Domain.Exceptions;

public sealed class PreconditionRequiredException(string message) : Exception(message);
