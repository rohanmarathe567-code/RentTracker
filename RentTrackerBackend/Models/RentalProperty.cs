using MongoDB.Bson.Serialization.Attributes;
using RentTrackerBackend.Models.Auth;

namespace RentTrackerBackend.Models
{
    public class RentalProperty : BaseDocument
    {
        public string? UserId { get; set; }
        
        [BsonIgnore]
        public User? User { get; set; }
        
        public Address Address { get; set; } = new Address();
        
        public string? Description { get; set; }
        
        [BsonElement("RentAmount")]
        public decimal WeeklyRentAmount { get; set; }
        
        public LeaseDates LeaseDates { get; set; } = new LeaseDates();
        
        public string? PropertyManager { get; set; }
        
        public string? PropertyManagerContact { get; set; }
        
        public List<RentalPayment> RentalPayments { get; set; } = new List<RentalPayment>();
        
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }
}