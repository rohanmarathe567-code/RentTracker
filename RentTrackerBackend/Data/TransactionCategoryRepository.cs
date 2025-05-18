using MongoDB.Driver;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data
{
    public interface ITransactionCategoryRepository : ISharedMongoRepository<PropertyTransactionCategory>
    {
    }

    public class TransactionCategoryRepository : SharedMongoRepository<PropertyTransactionCategory>, ITransactionCategoryRepository
    {
        public TransactionCategoryRepository(IMongoDatabase database) : base(database)
        {
        }
    }
}
