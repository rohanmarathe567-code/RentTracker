using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data
{
    public interface IPaymentRepository : IMongoRepository<RentalPayment>
    {
        Task<RentalPayment?> GetByPropertyIdAsync(string tenantId, string propertyId);
    }

    public class PaymentRepository : MongoRepository<RentalPayment>, IPaymentRepository
    {
        private readonly IMongoCollection<RentalPayment> _collection;
        private readonly IMongoCollection<PaymentMethod> _paymentMethodCollection;

        public PaymentRepository(IMongoClient client, IOptions<MongoDbSettings> settings) : base(client, settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<RentalPayment>(typeof(RentalPayment).Name);
            _paymentMethodCollection = database.GetCollection<PaymentMethod>(typeof(PaymentMethod).Name);
        }

        protected override async Task<IEnumerable<RentalPayment>> IncludeRelatedDataAsync(IEnumerable<RentalPayment> payments, string[]? includes)
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

        public async Task<RentalPayment?> GetByPropertyIdAsync(string tenantId, string propertyId)
        {
            return await _collection.Find(x => 
                x.TenantId == tenantId && 
                x.RentalPropertyId == propertyId)
                .FirstOrDefaultAsync();
        }
    }
}