using NobaRental.Backend.Domain.Models;

namespace NobaRental.Backend.Domain;

public interface ICarCategoryApi
{
    Task<IReadOnlyCollection<CarCategory>> GetCategories(bool? activeOnly = true, CancellationToken cancellationToken = default);
    Task<CarCategory?> GetCategory(string code, CancellationToken cancellationToken = default);
    Task<CarCategory> CreateCategory(string code, string name, decimal dayMultiplier, decimal kmMultiplier, bool chargesKilometers, CancellationToken cancellationToken = default);
    Task<CarCategory> UpdateCategory(string code, string name, decimal dayMultiplier, decimal kmMultiplier, bool chargesKilometers, bool isActive, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task DeleteCategory(string code, CancellationToken cancellationToken = default);
}
