using RentTrackerBackend.Models.Auth;

namespace RentTrackerBackend.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<bool> IsEmailUniqueAsync(string email);
    string GenerateToken(User user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}