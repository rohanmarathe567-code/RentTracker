namespace RentTrackerClient.Models;

public class Attachment
{
    public Guid Id { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    
    public string? ContentType { get; set; }
    
    public string? FilePath { get; set; }
    
    public long FileSize { get; set; }
    
    public string? Description { get; set; }
    
    public DateTime UploadDate { get; set; }
    
    public Guid? RentalPropertyId { get; set; }
    
    public Guid? RentalPaymentId { get; set; }
    
    public RentalProperty? RentalProperty { get; set; }
    
    public RentalPayment? RentalPayment { get; set; }
}