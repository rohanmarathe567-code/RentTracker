using MongoDB.Bson.Serialization.Attributes;

namespace RentTrackerBackend.Models
{
    public class RentalPayment : BaseDocument
    {
        [BsonElement("rentalPropertyId")]
        public string RentalPropertyId { get; set; } = string.Empty;
        
        [BsonIgnore]
        public RentalProperty? RentalProperty { get; set; }
        
        [BsonElement("amount")]
        public decimal Amount { get; set; }
        
        [BsonElement("paymentDate")]
        public DateTime PaymentDate { get; set; }
        
        [BsonElement("paymentMethodId")]
        public string? PaymentMethodId { get; set; }
        
        [BsonIgnore]
        public PaymentMethod? PaymentMethod { get; set; }
        
        [BsonElement("paymentReference")]
        public string? PaymentReference { get; set; }
        
        [BsonElement("notes")]
        public string? Notes { get; set; }
    }
}