using Microsoft.Extensions.Options;
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

        public PaymentRepository(IMongoClient client, IOptions<MongoDbSettings> settings) : base(client, settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<RentalPayment>(typeof(RentalPayment).Name);
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