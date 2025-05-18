using MongoDB.Driver;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data
{
    public interface IPaymentMethodRepository : ISharedMongoRepository<PaymentMethod>
    {
    }

    public class PaymentMethodRepository : SharedMongoRepository<PaymentMethod>, IPaymentMethodRepository
    {
        public PaymentMethodRepository(IMongoDatabase database) : base(database)
        {
        }
    }
}