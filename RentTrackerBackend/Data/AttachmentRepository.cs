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

            // Create indexes asynchronously
            InitializeIndexes().GetAwaiter().GetResult();
        }        private async Task InitializeIndexes()
        {
            // Create tenant indexes
            await _collection.CreateTenantIndexesAsync(CancellationToken.None);

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
            await _collection.Indexes.CreateManyAsync(indexes);
        }

        public Task<IEnumerable<Attachment>> GetAllAsync(string tenantId, string[]? includes = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));

            return _collection.GetAllAsync(tenantId);
        }

        public Task<Attachment> GetByIdAsync(string tenantId, string id)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            try
            {
                return _collection.GetByIdAsync(tenantId, id);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid ID format", nameof(id), ex);
            }
        }

        public Task<Attachment> CreateAsync(Attachment entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return _collection.CreateAsync(entity);
        }

        public Task UpdateAsync(string tenantId, string id, Attachment entity)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return _collection.UpdateAsync(tenantId, id, entity);
        }

        public Task DeleteAsync(string tenantId, string id)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            return _collection.DeleteAsync(tenantId, id);
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentsByPropertyIdAsync(string tenantId, string propertyId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(propertyId))
                throw new ArgumentException("Property ID cannot be null or empty", nameof(propertyId));

            return await _collection
                .Find(x => x.TenantId == tenantId && x.RentalPropertyId == propertyId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentsByPaymentIdAsync(string tenantId, string paymentId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(paymentId))
                throw new ArgumentException("Payment ID cannot be null or empty", nameof(paymentId));

            return await _collection
                .Find(x => x.TenantId == tenantId && x.RentalPaymentId == paymentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentsByEntityTypeAsync(string tenantId, string entityType)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));

            return await _collection
                .Find(x => x.TenantId == tenantId && x.EntityType == entityType)
                .ToListAsync();
        }
    }
}