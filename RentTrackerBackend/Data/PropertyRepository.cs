using MongoDB.Driver;
using Microsoft.Extensions.Options;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data
{
    public interface IPropertyRepository : IMongoRepository<RentalProperty>
    {
        Task<IEnumerable<RentalProperty>> GetPropertiesByCityAsync(string tenantId, string city);
        Task<IEnumerable<RentalProperty>> GetPropertiesByRentRangeAsync(string tenantId, decimal minRent, decimal maxRent);
        Task<IEnumerable<RentalProperty>> SearchPropertiesAsync(string tenantId, string searchText);
    }

    public class PropertyRepository : MongoRepository<RentalProperty>, IPropertyRepository
    {
        private readonly IMongoCollection<RentalProperty> _collection;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

        public PropertyRepository(
            IMongoClient mongoClient, 
            IOptions<MongoDbSettings> settings) : base(mongoClient, settings)
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<RentalProperty>(typeof(RentalProperty).Name);

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
                              .Ascending("Address.City")),
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending("LeaseDates.StartDate")
                              .Ascending("LeaseDates.EndDate")),
                
                // Text index for search functionality
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Text("Address.Street")
                              .Text("Address.City")
                              .Text("Address.State")),
                
                // Index for optimistic concurrency
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending(x => x.Version)),

                // Index for date-based queries
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending(x => x.UpdatedAt)),

                // Index for complex queries combining multiple fields
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending("Address.City")
                              .Ascending(x => x.RentAmount))
            };
            _collection.Indexes.CreateMany(indexes);
        }

        public override async Task<RentalProperty> GetByIdAsync(string tenantId, string id)
        {
            var property = await base.GetByIdAsync(tenantId, id);
            return property;
        }

        public override async Task<RentalProperty> CreateAsync(RentalProperty entity)
        {
            var property = await base.CreateAsync(entity);
            return property;
        }

        public async Task<IEnumerable<RentalProperty>> GetPropertiesByCityAsync(string tenantId, string city)
        {
            return await _collection
                .Find(x => x.TenantId == tenantId && x.Address.City == city)
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