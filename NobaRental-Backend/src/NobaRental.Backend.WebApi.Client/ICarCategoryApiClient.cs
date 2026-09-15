using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using RestEase;

namespace NobaRental.Backend.WebApi.Client;

public interface ICarCategoryApiClient
{
    [Get("api/v1/categories")]
    [AllowAnyStatusCode]
    Task<Response<IReadOnlyCollection<CarCategoryResponse>>> GetCategories(
        [Query] bool? onlyActive = true,
        CancellationToken cancellationToken = default);

    [Get("api/v1/categories/{code}")]
    [AllowAnyStatusCode]
    Task<Response<CarCategoryResponse>> GetCategoryByCode(
        [Path] string code,
        CancellationToken cancellationToken = default);

    [Post("api/v1/categories")]
    [AllowAnyStatusCode]
    Task<Response<CarCategoryResponse>> CreateCategory(
        [Body] CreateCarCategoryRequest request,
        CancellationToken cancellationToken = default);

    [Put("api/v1/categories/{code}")]
    [AllowAnyStatusCode]
    Task<Response<CarCategoryResponse>> UpdateCategory(
        [Path] string code,
        [Body] UpdateCarCategoryRequest request,
        [Header("If-Match")] string? ifMatch = null,
        CancellationToken cancellationToken = default);

    [Delete("api/v1/categories/{code}")]
    [AllowAnyStatusCode]
    Task<Response<string>> DeleteCategory(
        [Path] string code,
        CancellationToken cancellationToken = default);
}
