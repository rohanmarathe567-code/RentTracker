using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data
{
    // Interface for models that can be accessed across tenants
    public interface ISharedMongoRepository<T> : IMongoRepository<T> where T : BaseDocument
    {
        // Gets all records across all tenants
        Task<IEnumerable<T>> GetAllSharedAsync();
        
        // Gets a specific record regardless of tenant
        Task<T?> GetSharedByIdAsync(string id);
    }
}