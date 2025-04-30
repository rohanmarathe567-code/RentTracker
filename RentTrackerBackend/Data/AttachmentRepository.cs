using MongoDB.Driver;
using Microsoft.Extensions.Options;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data
{
    public interface IAttachmentRepository
    {
        Task<IEnumerable<Attachment>> GetAllAsync(string tenantId, string[]? includes = null);
        Task<Attachment> GetByIdAsync(string tenantId, string id);
        Task<Attachment> CreateAsync(Attachment entity);
        Task UpdateAsync(string tenantId, string id, Attachment entity);
        Task DeleteAsync(string tenantId, string id);
        Task<IEnumerable<Attachment>> GetAttachmentsByPropertyIdAsync(string tenantId, string propertyId);
        Task<IEnumerable<Attachment>> GetAttachmentsByPaymentIdAsync(string tenantId, string paymentId);
        Task<IEnumerable<Attachment>> GetAttachmentsByEntityTypeAsync(string tenantId, string entityType);
    }

    public class AttachmentRepository : IAttachmentRepository
    {
        private readonly IMongoCollection<Attachment> _collection;

        public AttachmentRepository(IMongoClient mongoClient, IOptions<MongoDbSettings> settings)
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<Attachment>(typeof(Attachment).Name);

            // Create indexes
            _collection.CreateTenantIndexes();

            // Create attachment-specific indexes
            var indexBuilder = Builders<Attachment>.IndexKeys;
            var indexes = new List<CreateIndexModel<Attachment>>
            {
                // Optimized compound indexes for common queries
                new CreateIndexModel<Attachment>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending(x => x.RentalPropertyId)),
                
                new CreateIndexModel<Attachment>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending(x => x.RentalPaymentId)),

                new CreateIndexModel<Attachment>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending(x => x.EntityType)),

                // Index for date-based queries
                new CreateIndexModel<Attachment>(
                    indexBuilder.Ascending(x => x.TenantId)
                              .Ascending(x => x.UpdatedAt))
            };
            _collection.Indexes.CreateMany(indexes);
        }

        public Task<IEnumerable<Attachment>> GetAllAsync(string tenantId, string[]? includes = null)
        {
            return _collection.GetAllAsync(tenantId);
        }

        public Task<Attachment> GetByIdAsync(string tenantId, string id)
        {
            return _collection.GetByIdAsync(tenantId, id);
        }

        public Task<Attachment> CreateAsync(Attachment entity)
        {
            return _collection.CreateAsync(entity);
        }

        public Task UpdateAsync(string tenantId, string id, Attachment entity)
        {
            return _collection.UpdateAsync(tenantId, id, entity);
        }

        public Task DeleteAsync(string tenantId, string id)
        {
            return _collection.DeleteAsync(tenantId, id);
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentsByPropertyIdAsync(string tenantId, string propertyId)
        {
            return await _collection
                .Find(x => x.TenantId == tenantId && x.RentalPropertyId == propertyId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentsByPaymentIdAsync(string tenantId, string paymentId)
        {
            return await _collection
                .Find(x => x.TenantId == tenantId && x.RentalPaymentId == paymentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentsByEntityTypeAsync(string tenantId, string entityType)
        {
            return await _collection
                .Find(x => x.TenantId == tenantId && x.EntityType == entityType)
                .ToListAsync();
        }
    }
}