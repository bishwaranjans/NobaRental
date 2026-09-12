namespace NobaRental.Backend.Domain.Models;

public sealed record Station(
    string Code,
    string Name,
    string City,
    bool IsActive,
    bool IsDeleted = false,
    byte[]? RowVersion = null);
