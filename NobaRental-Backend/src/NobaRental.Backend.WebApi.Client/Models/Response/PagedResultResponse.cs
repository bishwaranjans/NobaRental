namespace NobaRental.Backend.WebApi.Client.Models.Response;

public sealed record PagedResultResponse<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
