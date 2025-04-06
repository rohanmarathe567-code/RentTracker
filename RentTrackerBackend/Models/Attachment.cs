using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
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
    [Required]
    public string ContentType { get; set; } = string.Empty;
    
    [StringLength(1000)]
    [Required]
    public string StoragePath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(50)]
    [Required]
    public string EntityType { get; set; } = string.Empty;  // 'Property' or 'Payment'
    
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    
    public string[]? Tags { get; set; }
    
    // Can be associated with either a property or a payment
    public Guid? RentalPropertyId { get; set; }
    
    public Guid? RentalPaymentId { get; set; }
    
}