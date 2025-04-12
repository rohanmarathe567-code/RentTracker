using MongoDB.Bson.Serialization.Attributes;

namespace RentTrackerBackend.Models
{
    public class RentalPayment : BaseDocument
    {
        public string RentalPropertyId { get; set; } = string.Empty;
        
        [BsonIgnore]
        public RentalProperty? RentalProperty { get; set; }
        
        public decimal Amount { get; set; }
        
        public DateTime PaymentDate { get; set; }
        
        public string? PaymentMethodId { get; set; }
        
        [BsonIgnore]
        public PaymentMethod? PaymentMethod { get; set; }
        
        public string? PaymentReference { get; set; }
        
        public string? Notes { get; set; }
    }
}