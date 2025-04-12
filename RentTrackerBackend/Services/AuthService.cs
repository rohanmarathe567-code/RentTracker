using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models.Auth;
using MongoDB.Driver;

namespace RentTrackerBackend.Services;

public class AuthService : IAuthService
{
    private readonly IMongoCollection<User> _users;
    private readonly JwtConfig _jwtConfig;
    
    public AuthService(IMongoClient client, JwtConfig jwtConfig)
    {
        var database = client.GetDatabase("renttracker");
        _users = database.GetCollection<User>(nameof(User));
        _jwtConfig = jwtConfig;
    }
    
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (!await IsEmailUniqueAsync(request.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }
        
        var user = new User
        {
            TenantId = request.Email, // Using email as tenant ID for simplicity
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            UserType = UserType.User // Always create regular users through registration
        };
        
        await _users.InsertOneAsync(user);
        
        var token = GenerateToken(user);
        
        return new AuthResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email,
            UserType = user.UserType,
            Token = token,
            TokenExpiration = DateTime.UtcNow.AddMinutes(_jwtConfig.TokenLifetimeMinutes)
        };
    }
    
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
            
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password");
        }
        
        var token = GenerateToken(user);
        
        return new AuthResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email,
            UserType = user.UserType,
            Token = token,
            TokenExpiration = DateTime.UtcNow.AddMinutes(_jwtConfig.TokenLifetimeMinutes)
        };
    }
    
    public async Task<bool> IsEmailUniqueAsync(string email)
    {
        return await _users.Find(u => u.Email == email).CountDocumentsAsync() == 0;
    }
    
    public string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.UserType.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtConfig.TokenLifetimeMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}