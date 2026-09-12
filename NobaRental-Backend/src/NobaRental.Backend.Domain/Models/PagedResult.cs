namespace NobaRental.Backend.Domain.Models;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
