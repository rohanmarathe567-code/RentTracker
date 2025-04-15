using MongoDB.Bson;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace RentTrackerBackend.Models
{
    public class RentalProperty : BaseDocument
    {
        [BsonElement("address")]
        public Address Address { get; set; } = new Address();

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("propertyManager")]
        public PropertyManager PropertyManager { get; set; } = new PropertyManager();

        [BsonElement("rentAmount")]
        public decimal RentAmount { get; set; }

        [BsonElement("leaseDates")]
        public LeaseDates LeaseDates { get; set; } = new LeaseDates();

        [BsonElement("paymentIds")]
        public List<string> PaymentIds { get; set; } = new List<string>();

        [BsonElement("attachmentIds")]
        public List<string> AttachmentIds { get; set; } = new List<string>();

        [BsonIgnore]
        public List<RentalPayment> Payments { get; set; } = new List<RentalPayment>();
        
        [BsonIgnore]
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    }

    public class PropertyManager
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("contact")]
        public string Contact { get; set; } = string.Empty;
    }

    public class Address
    {
        [BsonElement("street")]
        public string Street { get; set; } = string.Empty;

        [BsonElement("city")]
        public string City { get; set; } = string.Empty;

        [BsonElement("state")]
        public string State { get; set; } = string.Empty;

        [BsonElement("zipCode")]
        public string ZipCode { get; set; } = string.Empty;
    }

    public class LeaseDates
    {
        [BsonElement("startDate")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [BsonElement("endDate")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddYears(1);
    }
}