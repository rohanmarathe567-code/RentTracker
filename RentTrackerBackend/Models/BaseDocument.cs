using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RentTrackerBackend.Models
{
    public class BaseDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
    }

    public class LeaseDates
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}