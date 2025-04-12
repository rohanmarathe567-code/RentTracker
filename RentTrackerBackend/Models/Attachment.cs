using MongoDB.Bson.Serialization.Attributes;

namespace RentTrackerBackend.Models;

public class Attachment : BaseDocument
{
    public string FileName { get; set; } = string.Empty;
    
    public string ContentType { get; set; } = string.Empty;
    
    public string StoragePath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public string? Description { get; set; }
    
    public string EntityType { get; set; } = string.Empty;  // 'Property' or 'Payment'
    
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    
    public string[]? Tags { get; set; }
    
    public string? RentalPropertyId { get; set; }
    
    [BsonIgnore]
    public RentalProperty? RentalProperty { get; set; }
    
    public string? RentalPaymentId { get; set; }
    
    [BsonIgnore]
    public RentalPayment? RentalPayment { get; set; }
}