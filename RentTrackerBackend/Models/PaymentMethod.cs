using System.ComponentModel.DataAnnotations;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Models;

public class PaymentMethod
{
    [Key]
    public Guid Id { get; set; } = SequentialGuidGenerator.NewSequentialGuid();
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public bool IsSystemDefault { get; set; }
    
    public Guid? UserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}