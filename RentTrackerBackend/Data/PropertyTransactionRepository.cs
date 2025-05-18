using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data
{    public interface IPropertyTransactionRepository
    {
        Task<IEnumerable<PropertyTransaction>> GetAllAsync(string tenantId, bool includeSystem = false, string[]? includes = null);
        Task<PropertyTransaction?> GetByIdAsync(string tenantId, string id);
        Task<PropertyTransaction> CreateAsync(PropertyTransaction entity);
        Task UpdateAsync(string tenantId, string id, PropertyTransaction entity);
        Task DeleteAsync(string tenantId, string id);
        Task<List<PropertyTransaction>> GetByPropertyIdAsync(string tenantId, string propertyId);
    }

    public class PropertyTransactionRepository : IPropertyTransactionRepository
    {
        private readonly IMongoCollection<PropertyTransaction> _collection;
        private readonly IMongoCollection<PaymentMethod> _paymentMethodCollection;
        private readonly IMongoCollection<PropertyTransactionCategory> _categoryCollection;
        private readonly IMongoCollection<Attachment> _attachmentCollection;

        public PropertyTransactionRepository(IMongoClient client, IOptions<MongoDbSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<PropertyTransaction>(nameof(PropertyTransaction));
            _paymentMethodCollection = database.GetCollection<PaymentMethod>(nameof(PaymentMethod));
            _categoryCollection = database.GetCollection<PropertyTransactionCategory>(nameof(PropertyTransactionCategory));
            _attachmentCollection = database.GetCollection<Attachment>(nameof(Attachment));
            
            // Initialize indexes asynchronously
            InitializeIndexes().GetAwaiter().GetResult();
        }
        
        private async Task InitializeIndexes()
        {
            // Create base tenant indexes
            await _collection.CreateTenantIndexesAsync(CancellationToken.None);
            
            // Create additional indexes
            var indexOptions = new CreateIndexOptions { Background = true };
            var propertyIdIndex = Builders<PropertyTransaction>.IndexKeys.Ascending(x => x.RentalPropertyId);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<PropertyTransaction>(propertyIdIndex, indexOptions));
            
            var categoryIdIndex = Builders<PropertyTransaction>.IndexKeys.Ascending(x => x.CategoryId);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<PropertyTransaction>(categoryIdIndex, indexOptions));
            
            var transactionTypeIndex = Builders<PropertyTransaction>.IndexKeys.Ascending(x => x.TransactionType);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<PropertyTransaction>(transactionTypeIndex, indexOptions));
            
            var transactionDateIndex = Builders<PropertyTransaction>.IndexKeys.Ascending(x => x.TransactionDate);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<PropertyTransaction>(transactionDateIndex, indexOptions));
        }

        public async Task<IEnumerable<PropertyTransaction>> GetAllAsync(string tenantId, bool includeSystem = false, string[]? includes = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            }

            var transactions = await _collection.GetAllAsync(tenantId, includeSystem);
            var transactionsList = transactions.ToList();
            
            await IncludeReferencedEntitiesAsync(transactionsList, includes);
            
            return transactionsList;
        }

        private async Task IncludeReferencedEntitiesAsync(List<PropertyTransaction> transactions, string[]? includes)
        {
            // Include payment methods if requested
            if (includes?.Contains("PaymentMethod") == true)
            {
                var paymentMethodIds = transactions
                    .Where(p => !string.IsNullOrEmpty(p.PaymentMethodId))
                    .Select(p => ObjectId.Parse(p.PaymentMethodId))
                    .Distinct()
                    .ToList();

                if (paymentMethodIds.Any())
                {
                    var paymentMethods = await _paymentMethodCollection
                        .Find(x => paymentMethodIds.Contains(x.Id))
                        .ToListAsync();

                    var methodsDict = paymentMethods.ToDictionary(m => m.Id.ToString());

                    foreach (var transaction in transactions)
                    {
                        if (!string.IsNullOrEmpty(transaction.PaymentMethodId) &&
                            methodsDict.TryGetValue(transaction.PaymentMethodId, out var method))
                        {
                            transaction.PaymentMethod = method;
                        }
                    }
                }
            }
            
            // Include categories if requested
            if (includes?.Contains("Category") == true)
            {
                var categoryIds = transactions
                    .Select(t => ObjectId.Parse(t.CategoryId))
                    .Distinct()
                    .ToList();

                if (categoryIds.Any())
                {
                    var categories = await _categoryCollection
                        .Find(x => categoryIds.Contains(x.Id))
                        .ToListAsync();

                    var categoriesDict = categories.ToDictionary(c => c.Id.ToString());

                    foreach (var transaction in transactions)
                    {
                        if (categoriesDict.TryGetValue(transaction.CategoryId, out var category))
                        {
                            transaction.Category = category;
                        }
                    }
                }
            }
            
            // Include attachments if requested
            if (includes?.Contains("Attachments") == true)
            {
                var attachmentIds = transactions
                    .SelectMany(t => t.AttachmentIds)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Select(id => ObjectId.Parse(id))
                    .Distinct()
                    .ToList();

                if (attachmentIds.Any())
                {
                    var attachments = await _attachmentCollection
                        .Find(x => attachmentIds.Contains(x.Id))
                        .ToListAsync();

                    var attachmentsDict = attachments.ToDictionary(a => a.Id.ToString());

                    foreach (var transaction in transactions)
                    {
                        transaction.Attachments = new List<Attachment>();
                        foreach (var attachmentId in transaction.AttachmentIds)
                        {
                            if (attachmentsDict.TryGetValue(attachmentId, out var attachment))
                            {
                                transaction.Attachments.Add(attachment);
                            }
                        }
                    }
                }
            }
        }        public async Task<PropertyTransaction?> GetByIdAsync(string tenantId, string id)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            }

            if (!ObjectId.TryParse(id, out _))
            {
                throw new FormatException("Invalid ObjectId format");
            }

            var transaction = await _collection.GetByIdAsync(tenantId, id);
            
            if (transaction != null)
            {
                await IncludeReferencedEntitiesAsync(new List<PropertyTransaction> { transaction }, 
                    new[] { "PaymentMethod", "Category", "Attachments" });
            }
            
            return transaction;
        }

        public async Task<PropertyTransaction> CreateAsync(PropertyTransaction entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (entity.Amount <= 0)
            {
                throw new ArgumentException("Transaction amount must be greater than zero", nameof(entity));
            }

            return await _collection.CreateAsync(entity);
        }

        public async Task UpdateAsync(string tenantId, string id, PropertyTransaction entity)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            }

            if (!ObjectId.TryParse(id, out _))
            {
                throw new FormatException("Invalid ObjectId format");
            }

            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await _collection.UpdateAsync(tenantId, id, entity);
        }

        public async Task DeleteAsync(string tenantId, string id)
        {
            await _collection.DeleteAsync(tenantId, id);
        }

        public async Task<List<PropertyTransaction>> GetByPropertyIdAsync(string tenantId, string propertyId)
        {
            var transactions = await _collection.Find(x =>
                x.TenantId == tenantId &&
                x.RentalPropertyId == propertyId)
                .ToListAsync();
            
            // Include all related entities
            await IncludeReferencedEntitiesAsync(transactions, 
                new[] { "PaymentMethod", "Category", "Attachments" });
            
            return transactions;
        }
    }
}
