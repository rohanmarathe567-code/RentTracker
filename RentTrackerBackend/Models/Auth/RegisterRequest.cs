using System.ComponentModel.DataAnnotations;

namespace RentTrackerBackend.Models.Auth;

/// <summary>
/// Request model for user registration. Creates a regular user account.
/// Admin accounts are managed through system configuration.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's first name. Required field.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's middle name. Optional field.
    /// </summary>
    [StringLength(50)]
    public string? MiddleName { get; set; }

    /// <summary>
    /// User's last name. Required field.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address. Must be a valid email format.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User's password. Must be between 6 and 100 characters.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Confirmation of the password. Must match the Password field.
    /// </summary>
    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}