using MongoDB.Driver;
using RentTrackerBackend.Models;
using MongoDB.Bson;

namespace RentTrackerBackend.Data
{
    public class SharedMongoRepository<T> : MongoRepository<T>, ISharedMongoRepository<T> where T : BaseDocument
    {
        private readonly IMongoCollection<T> _collection;

        public SharedMongoRepository(IMongoDatabase database) : base(database)
        {
            _collection = database.GetCollection<T>(typeof(T).Name);
        }

        public async Task<IEnumerable<T>> GetAllSharedAsync()
        {
            // Get all records for both system and all tenants
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<T?> GetSharedByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                throw new FormatException($"Invalid ObjectId format: {id}");

            var objId = ObjectId.Parse(id);

            // First try to find a system record with this ID
            var result = await _collection.Find(x => 
                x.Id == objId && 
                x.TenantId == "system"
            ).FirstOrDefaultAsync();

            if (result == null)
            {
                // If no system record found, look for any tenant's record
                result = await _collection.Find(x => x.Id == objId).FirstOrDefaultAsync();
            }

            return result;
        }
    }
}