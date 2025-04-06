using System.ComponentModel.DataAnnotations;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Models;

public class RentalProperty
{
    [Key]
    public Guid Id { get; set; } = SequentialGuidGenerator.NewSequentialGuid();
    
    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? Suburb { get; set; }
    
    [StringLength(50)]
    public string? State { get; set; }
    
    [StringLength(10)]
    public string? PostCode { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public decimal? WeeklyRentAmount { get; set; }
    
    public DateTime? LeaseStartDate { get; set; }
    
    public DateTime? LeaseEndDate { get; set; }
    
    [StringLength(100)]
    public string? PropertyManager { get; set; }
    
    [StringLength(100)]
    public string? PropertyManagerContact { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
}