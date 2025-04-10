using System.ComponentModel.DataAnnotations;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Models.Auth;

public class User
{
    [Key]
    public Guid Id { get; set; } = SequentialGuidGenerator.NewSequentialGuid();
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    public UserType UserType { get; set; } = UserType.User;
    
    // Navigation property for owned properties
    public virtual ICollection<RentalProperty> Properties { get; set; } = new List<RentalProperty>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}