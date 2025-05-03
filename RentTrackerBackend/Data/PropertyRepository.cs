using MongoDB.Driver;
using Microsoft.Extensions.Options;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data
{
    public interface IPropertyRepository
    {
        Task<IEnumerable<RentalProperty>> GetAllAsync(string tenantId, string[]? includes = null);
        Task<RentalProperty> GetByIdAsync(string tenantId, string id);
        Task<RentalProperty> CreateAsync(RentalProperty? entity);
        Task UpdateAsync(string tenantId, string id, RentalProperty entity);
        Task DeleteAsync(string tenantId, string id);
        Task<IEnumerable<RentalProperty>> GetPropertiesByCityAsync(string tenantId, string city);
        Task<IEnumerable<RentalProperty>> GetPropertiesByRentRangeAsync(string tenantId, decimal minRent, decimal maxRent);
        Task<IEnumerable<RentalProperty>> SearchPropertiesAsync(string tenantId, string searchText);
    }

    public class PropertyRepository : IPropertyRepository
    {
        private readonly IMongoCollection<RentalProperty> _collection;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

        public PropertyRepository(IMongoClient mongoClient, IOptions<MongoDbSettings> settings)
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<RentalProperty>(typeof(RentalProperty).Name);

            // Initialize indexes asynchronously
            InitializeIndexes().GetAwaiter().GetResult();
        }        private async Task InitializeIndexes()
        {
            // Create base tenant indexes
            await _collection.CreateTenantIndexesAsync(CancellationToken.None);

            // Create property-specific indexes
            var indexBuilder = Builders<RentalProperty>.IndexKeys;
            var indexes = new List<CreateIndexModel<RentalProperty>>
            {
                // Optimized compound indexes for common queries
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending(x => x.RentAmount)),
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending("LeaseDates.StartDate")
                              .Ascending("LeaseDates.EndDate")),
                
                // Text index for search functionality
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Text("Address.Street")
                              .Text("Address.City")
                              .Text("Address.State")),

                // Index for date-based queries
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending(x => x.UpdatedAt))
            };
            await _collection.Indexes.CreateManyAsync(indexes);
        }

        public Task<IEnumerable<RentalProperty>> GetAllAsync(string tenantId, string[]? includes = null)
        {
            return _collection.GetAllAsync(tenantId);
        }

        public Task<RentalProperty> GetByIdAsync(string tenantId, string id)
        {
            return _collection.GetByIdAsync(tenantId, id);
        }
        public Task<RentalProperty> CreateAsync(RentalProperty? entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
                
            return _collection.CreateAsync(entity);
        }

        public Task UpdateAsync(string tenantId, string id, RentalProperty entity)
        {
            return _collection.UpdateAsync(tenantId, id, entity);
        }

        public Task DeleteAsync(string tenantId, string id)
        {
            return _collection.DeleteAsync(tenantId, id);
        }

        public async Task<IEnumerable<RentalProperty>> GetPropertiesByCityAsync(string tenantId, string city)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City cannot be null or empty", nameof(city));

            return await _collection
                .Find(x => x.TenantId == tenantId && 
                          x.Address != null && 
                          x.Address.City == city)
                .ToListAsync();
        }

        public async Task<IEnumerable<RentalProperty>> GetPropertiesByRentRangeAsync(string tenantId, decimal minRent, decimal maxRent)
        {
            return await _collection
                .Find(x => x.TenantId == tenantId && 
                          x.RentAmount >= minRent && 
                          x.RentAmount <= maxRent)
                .ToListAsync();
        }

        public async Task<IEnumerable<RentalProperty>> SearchPropertiesAsync(string tenantId, string searchText)
        {
            // Create a text search filter combined with tenant filter
            var builder = Builders<RentalProperty>.Filter;
            var filter = builder.And(
                builder.Eq(x => x.TenantId, tenantId),
                builder.Text(searchText)
            );

            return await _collection
                .Find(filter)
                .SortByDescending(x => x.UpdatedAt)
                .ToListAsync();
        }
    }
}