using MongoDB.Bson.Serialization.Attributes;

namespace RentTrackerBackend.Models
{
    public class PropertyTransactionCategory : BaseDocument
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("description")]
        public string? Description { get; set; }
        
        [BsonElement("transactionType")]
        public TransactionType TransactionType { get; set; }
        
        [BsonElement("isSystemDefault")]
        public bool IsSystemDefault { get; set; }
        
        [BsonElement("userId")]
        public string? UserId { get; set; }
        
        [BsonElement("order")]
        public int Order { get; set; }
    }
    
    public enum TransactionType
    {
        Income,
        Expense
    }
}
