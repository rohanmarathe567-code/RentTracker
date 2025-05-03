using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<RentalPayment>> GetAllAsync(string tenantId, bool includeSystem = false, string[]? includes = null);
        Task<RentalPayment> GetByIdAsync(string tenantId, string id);
        Task<RentalPayment> CreateAsync(RentalPayment entity);
        Task UpdateAsync(string tenantId, string id, RentalPayment entity);
        Task DeleteAsync(string tenantId, string id);
        Task<RentalPayment?> GetByPropertyIdAsync(string tenantId, string propertyId);
    }

    public class PaymentRepository : IPaymentRepository
    {
        private readonly IMongoCollection<RentalPayment> _collection;
        private readonly IMongoCollection<PaymentMethod> _paymentMethodCollection;

        public PaymentRepository(IMongoClient client, IOptions<MongoDbSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<RentalPayment>(nameof(RentalPayment));
            _paymentMethodCollection = database.GetCollection<PaymentMethod>(nameof(PaymentMethod));
            
            // Initialize indexes asynchronously
            InitializeIndexes().GetAwaiter().GetResult();
        }        private async Task InitializeIndexes()
        {
            // Create base tenant indexes
            await _collection.CreateTenantIndexesAsync(CancellationToken.None);
        }

        public async Task<IEnumerable<RentalPayment>> GetAllAsync(string tenantId, bool includeSystem = false, string[]? includes = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            }

            var payments = await _collection.GetAllAsync(tenantId, includeSystem);
            return await IncludePaymentMethodsAsync(payments.ToList(), includes);
        }

        private async Task<IEnumerable<RentalPayment>> IncludePaymentMethodsAsync(List<RentalPayment> payments, string[]? includes)
        {
            if (includes?.Contains("PaymentMethod") == true)
            {
                var paymentMethodIds = payments
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

                    foreach (var payment in payments)
                    {
                        if (!string.IsNullOrEmpty(payment.PaymentMethodId) &&
                            methodsDict.TryGetValue(payment.PaymentMethodId, out var method))
                        {
                            payment.PaymentMethod = method;
                        }
                    }
                }
            }

            return payments;
        }

        public async Task<RentalPayment> GetByIdAsync(string tenantId, string id)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            }

            if (!ObjectId.TryParse(id, out _))
            {
                throw new FormatException("Invalid ObjectId format");
            }

            return await _collection.GetByIdAsync(tenantId, id);
        }

        public async Task<RentalPayment> CreateAsync(RentalPayment entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (entity.Amount <= 0)
            {
                throw new ArgumentException("Payment amount must be greater than zero", nameof(entity));
            }

            return await _collection.CreateAsync(entity);
        }

        public async Task UpdateAsync(string tenantId, string id, RentalPayment entity)
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

        public async Task<RentalPayment?> GetByPropertyIdAsync(string tenantId, string propertyId)
        {
            return await _collection.Find(x =>
                x.TenantId == tenantId &&
                x.RentalPropertyId == propertyId)
                .FirstOrDefaultAsync();
        }
    }
}