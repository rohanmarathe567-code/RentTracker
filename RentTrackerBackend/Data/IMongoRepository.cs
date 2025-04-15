using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data
{
    public interface IMongoRepository<T> where T : BaseDocument
    {
        Task<T> GetByIdAsync(string tenantId, string id);
        Task<IEnumerable<T>> GetAllAsync(string tenantId, string[]? includes = null);
        Task<T> CreateAsync(T entity);
        Task UpdateAsync(string tenantId, string id, T entity);
        Task DeleteAsync(string tenantId, string id);
    }
}