using MongoDB.Bson;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace RentTrackerBackend.Models
{
    public class RentalProperty : BaseDocument
    {
        public Address Address { get; set; } = new Address();
        public PropertyManager PropertyManager { get; set; } = new PropertyManager();
        public decimal RentAmount { get; set; }
        public LeaseDates LeaseDates { get; set; } = new LeaseDates();
        public List<string> PaymentIds { get; set; } = new List<string>();
        public List<string> AttachmentIds { get; set; } = new List<string>();
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        [BsonIgnore]
        public List<Payment> Payments { get; set; } = new List<Payment>();
        
        [BsonIgnore]
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    }

    public class PropertyManager{
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
    }

    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }

    public class LeaseDates
    {
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddYears(1);
    }

    public class Payment : BaseDocument
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public string RentalPropertyId { get; set; } = string.Empty;

        [BsonIgnore]
        public RentalProperty? RentalProperty { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }
}