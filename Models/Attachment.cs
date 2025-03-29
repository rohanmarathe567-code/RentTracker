using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentTracker.Models;

public class Attachment
{
    [Key]
    public int Id { get; set; }
    
    [StringLength(255)]
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? ContentType { get; set; }
    
    [StringLength(1000)]
    public string? FilePath { get; set; }
    
    public long FileSize { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    
    // Can be associated with either a property or a payment
    public int? RentalPropertyId { get; set; }
    
    public int? RentalPaymentId { get; set; }
    
    // Navigation properties
    [ForeignKey("RentalPropertyId")]
    public RentalProperty? RentalProperty { get; set; }
    
    [ForeignKey("RentalPaymentId")]
    public RentalPayment? RentalPayment { get; set; }
}