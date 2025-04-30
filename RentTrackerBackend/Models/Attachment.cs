using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace RentTrackerBackend.Models
{
    public class Attachment : BaseDocument
    {
        [BsonElement("fileName")]
        public string FileName { get; set; } = string.Empty;
        
        [BsonElement("contentType")]
        public string ContentType { get; set; } = string.Empty;
        
        [BsonElement("storagePath")]
        public string StoragePath { get; set; } = string.Empty;
        
        [BsonElement("fileSize")]
        public long FileSize { get; set; }
        
        [BsonElement("description")]
        public string? Description { get; set; }
        
        [BsonElement("entityType")]
        public string EntityType { get; set; } = string.Empty;
        
        [BsonElement("uploadDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        
        [BsonElement("tags")]
        public string[]? Tags { get; set; }
        
        [BsonElement("rentalPropertyId")]
        public string? RentalPropertyId { get; set; }
        
        [BsonElement("rentalPaymentId")]
        public string? RentalPaymentId { get; set; }
    }
}