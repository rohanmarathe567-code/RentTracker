using MongoDB.Driver;
using RentTrackerBackend.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace RentTrackerBackend.Data
{
    public static class MongoDbExtensions
    {
        public static async Task<IEnumerable<T>> GetAllAsync<T>(this IMongoCollection<T> collection, string tenantId, bool includeSystem = false) where T : BaseDocument
        {
            var builder = Builders<T>.Filter;
            var filters = new List<FilterDefinition<T>>();

            // Add tenant-specific filter
            filters.Add(builder.Eq("tenantId", tenantId));

            // Include system-wide items if requested
            if (includeSystem)
            {
                filters.Add(builder.Eq("tenantId", "system"));
            }

            // Combine filters with OR if we're including system items
            var filter = includeSystem
                ? builder.Or(filters)
                : filters[0];
            
            return await collection.Find(filter).ToListAsync();
        }

        public static async Task<T> GetByIdAsync<T>(this IMongoCollection<T> collection, string tenantId, string id) where T : BaseDocument
        {
            if (!ObjectId.TryParse(id, out _))
                throw new FormatException($"Invalid ObjectId format: {id}");

            return await collection.Find(x =>
                x.TenantId == tenantId &&
                x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
        }

        public static async Task<T> CreateAsync<T>(this IMongoCollection<T> collection, T entity) where T : BaseDocument
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var now = DateTime.UtcNow;
            
            // Set initial values before insert
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            entity.Version = 1;

            // Use InsertOneAsync which is atomic
            await collection.InsertOneAsync(entity);
            
            return entity;
        }

        public static async Task UpdateAsync<T>(this IMongoCollection<T> collection, string tenantId, string id, T entity) where T : BaseDocument
        {
            if (!ObjectId.TryParse(id, out _))
                throw new FormatException($"Invalid ObjectId format: {id}");

            var now = DateTime.UtcNow;
            var currentVersion = entity.Version;

            // Update timestamps and version
            entity.UpdatedAt = now;
            entity.Version = currentVersion + 1;

            // Use ReplaceOneAsync for atomic update with optimistic concurrency
            var result = await collection.ReplaceOneAsync(
                x => x.TenantId == tenantId &&
                     x.Id == ObjectId.Parse(id) &&
                     x.Version == currentVersion,
                entity);

            if (result.ModifiedCount == 0)
            {
                // Rollback entity changes on conflict
                entity.UpdatedAt = DateTime.MinValue;
                entity.Version = currentVersion;
                throw new InvalidOperationException("Concurrency conflict - the document has been modified by another user.");
            }
        }        public static async Task DeleteAsync<T>(this IMongoCollection<T> collection, string tenantId, string id) where T : BaseDocument
        {
            if (!ObjectId.TryParse(id, out _))
                throw new FormatException($"Invalid ObjectId format: {id}");

            await collection.DeleteOneAsync(x => x.TenantId == tenantId && x.Id == ObjectId.Parse(id));
        }        
        
        public static async Task CreateTenantIndexesAsync<T>(this IMongoCollection<T> collection, CancellationToken cancellationToken = default) where T : BaseDocument
        {
            var indexBuilder = Builders<T>.IndexKeys;
            var indexes = new List<CreateIndexModel<T>>
            {
                new CreateIndexModel<T>(indexBuilder.Ascending(x => x.TenantId)),
                new CreateIndexModel<T>(indexBuilder.Ascending(x => x.TenantId).Ascending("Address.City"))
            };
            
            await collection.Indexes.CreateManyAsync(indexes, options: null, cancellationToken);
        }
    }

    public class MongoRepository<T> : IMongoRepository<T> where T : BaseDocument
    {
        private readonly IMongoCollection<T> _collection;        public MongoRepository(IMongoDatabase database)
        {
            // Ensure the class map is registered
            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                BsonClassMap.RegisterClassMap<T>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }

            var collectionName = typeof(T).Name;
            _collection = database.GetCollection<T>(collectionName);
            _collection.CreateTenantIndexesAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<T> GetByIdAsync(string tenantId, string id)
        {
            return await _collection.GetByIdAsync(tenantId, id);
        }

        public async Task<IEnumerable<T>> GetAllAsync(string tenantId, bool includeSystem = false, string[]? includes = null)
        {
            return await _collection.GetAllAsync(tenantId, includeSystem);
        }

        public async Task<T> CreateAsync(T entity)
        {
            return await _collection.CreateAsync(entity);
        }

        public async Task UpdateAsync(string tenantId, string id, T entity)
        {
            await _collection.UpdateAsync(tenantId, id, entity);
        }

        public async Task DeleteAsync(string tenantId, string id)
        {
            await _collection.DeleteAsync(tenantId, id);
        }
    }
}