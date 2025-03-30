using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Models;

public class Attachment
{
    [Key]
    public Guid Id { get; set; } = SequentialGuidGenerator.NewSequentialGuid();
    
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
    public Guid? RentalPropertyId { get; set; }
    
    public Guid? RentalPaymentId { get; set; }
    
    // Navigation properties
    [ForeignKey("RentalPropertyId")]
    public RentalProperty? RentalProperty { get; set; }
    
    [ForeignKey("RentalPaymentId")]
    public RentalPayment? RentalPayment { get; set; }
}