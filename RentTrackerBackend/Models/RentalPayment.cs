using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Models;

public class RentalPayment
{
    [Key]
    public Guid Id { get; set; } = SequentialGuidGenerator.NewSequentialGuid();
    
    [Required]
    public Guid RentalPropertyId { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [Required]
    public DateTime PaymentDate { get; set; }
    
    [StringLength(50)]
    public string? PaymentMethod { get; set; }
    
    [StringLength(100)]
    public string? PaymentReference { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("RentalPropertyId")]
    public RentalProperty? RentalProperty { get; set; }
    
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}