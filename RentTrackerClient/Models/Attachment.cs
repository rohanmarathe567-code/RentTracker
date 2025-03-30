namespace RentTrackerClient.Models;

public class Attachment
{
    public int Id { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    
    public string? ContentType { get; set; }
    
    public string? FilePath { get; set; }
    
    public long FileSize { get; set; }
    
    public string? Description { get; set; }
    
    public DateTime UploadDate { get; set; }
    
    public int? RentalPropertyId { get; set; }
    
    public int? RentalPaymentId { get; set; }
    
    public RentalProperty? RentalProperty { get; set; }
    
    public RentalPayment? RentalPayment { get; set; }
}