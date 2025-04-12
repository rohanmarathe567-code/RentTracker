namespace RentTrackerClient.Models;

public class Attachment
{
    public string Id { get; set; } = string.Empty;
    
    public string FileName { get; set; } = string.Empty;
    
    public string ContentType { get; set; } = string.Empty;
    
    public string StoragePath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public string? Description { get; set; }
    
    public string EntityType { get; set; } = string.Empty;
    
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    
    public string[]? Tags { get; set; }
    
    public string? RentalPropertyId { get; set; }
    
    public RentalProperty? RentalProperty { get; set; }
    
    public string? RentalPaymentId { get; set; }
    
    public RentalPayment? RentalPayment { get; set; }
}