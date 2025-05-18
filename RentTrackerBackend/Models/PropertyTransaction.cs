using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace RentTrackerBackend.Models
{
    public class PropertyTransaction : BaseDocument
    {
        [BsonElement("rentalPropertyId")]
        public string RentalPropertyId { get; set; } = string.Empty;
        
        [BsonIgnore]
        [JsonIgnore]
        public RentalProperty? RentalProperty { get; set; }
        
        [BsonElement("amount")]
        public decimal Amount { get; set; }
        
        [BsonElement("transactionDate")]
        public DateTime TransactionDate { get; set; }
        
        [BsonElement("transactionType")]
        public TransactionType TransactionType { get; set; } = TransactionType.Income;
        
        [BsonElement("categoryId")]
        public string CategoryId { get; set; } = string.Empty;
        
        [BsonIgnore]
        public PropertyTransactionCategory? Category { get; set; }
        
        [BsonElement("paymentMethodId")]
        public string? PaymentMethodId { get; set; }
        
        [BsonIgnore]
        public PaymentMethod? PaymentMethod { get; set; }
        
        [BsonElement("reference")]
        public string? Reference { get; set; }
        
        [BsonElement("notes")]
        public string? Notes { get; set; }
        
        [BsonElement("attachmentIds")]
        public List<string> AttachmentIds { get; set; } = new List<string>();
        
        [BsonIgnore]
        [JsonIgnore]
        public List<Attachment>? Attachments { get; set; }
    }
}
