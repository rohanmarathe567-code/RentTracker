namespace RentTrackerBackend.Models.Auth;

public class AuthResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public UserType UserType { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime TokenExpiration { get; set; }
}