using MongoDB.Bson.Serialization.Attributes;

namespace RentTrackerBackend.Models;

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
    public string EntityType { get; set; } = string.Empty;  // 'Property' or 'Payment'
    
    [BsonElement("uploadDate")]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    
    [BsonElement("tags")]
    public string[]? Tags { get; set; }
    
    [BsonElement("rentalPropertyId")]
    public string? RentalPropertyId { get; set; }
    
    [BsonIgnore]
    public RentalProperty? RentalProperty { get; set; }
    
    [BsonElement("rentalPaymentId")]
    public string? RentalPaymentId { get; set; }
    
    [BsonIgnore]
    public RentalPayment? RentalPayment { get; set; }
}