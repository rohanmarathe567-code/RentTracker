namespace RentTrackerBackend.Models.Auth;

public class AuthResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserType UserType { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime TokenExpiration { get; set; }
}