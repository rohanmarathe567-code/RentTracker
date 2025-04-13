using MongoDB.Driver;
using Microsoft.Extensions.Options;
using RentTrackerBackend.Models;
using MongoDB.Bson;

namespace RentTrackerBackend.Data
{
    public interface IMongoRepository<T> where T : BaseDocument
    {
        Task<IEnumerable<T>> GetAllAsync(string tenantId, string[]? includes = null);
        Task<T> GetByIdAsync(string tenantId, string id);
        Task<T> CreateAsync(T entity);
        Task UpdateAsync(string tenantId, string id, T entity);
        Task DeleteAsync(string tenantId, string id);
    }

    public class MongoRepository<T> : IMongoRepository<T> where T : BaseDocument
    {
        private readonly IMongoCollection<T> _collection;

        public MongoRepository(IMongoClient client, IOptions<MongoDbSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<T>(typeof(T).Name);
            
            // Create indexes
            var indexBuilder = Builders<T>.IndexKeys;
            var indexes = new List<CreateIndexModel<T>>
            {
                new CreateIndexModel<T>(indexBuilder.Ascending(x => x.TenantId)),
                new CreateIndexModel<T>(indexBuilder.Ascending(x => x.TenantId).Ascending("Address.City"))
            };
            _collection.Indexes.CreateMany(indexes);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(string tenantId, string[]? includes = null)
        {
            var items = await _collection.Find(x => x.TenantId == tenantId).ToListAsync();
            return await IncludeRelatedDataAsync(items, includes);
        }

        protected virtual async Task<IEnumerable<T>> IncludeRelatedDataAsync(IEnumerable<T> items, string[]? includes)
        {
            // Base implementation does nothing with includes
            // Derived classes should override this to handle specific includes
            return items;
        }

        public virtual async Task<T> GetByIdAsync(string tenantId, string id)
        {
            return await _collection.Find(x =>
                x.TenantId == tenantId &&
                x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
        }

        public virtual async Task<T> CreateAsync(T entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await _collection.InsertOneAsync(entity);
            return entity;
        }

        public virtual async Task UpdateAsync(string tenantId, string id, T entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            var currentVersion = entity.Version;
            entity.Version++;

            var result = await _collection.ReplaceOneAsync(
                x => x.TenantId == tenantId &&
                     x.Id == ObjectId.Parse(id) &&
                     x.Version == currentVersion,
                entity);

            if (result.ModifiedCount == 0)
            {
                throw new InvalidOperationException("Concurrency conflict - the document has been modified by another user.");
            }
        }

        public virtual async Task DeleteAsync(string tenantId, string id)
        {
            await _collection.DeleteOneAsync(
                x => x.TenantId == tenantId && x.Id == ObjectId.Parse(id));
        }
    }
}