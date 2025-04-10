using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models.Auth;

namespace RentTrackerBackend.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtConfig _jwtConfig;
    
    public AuthService(ApplicationDbContext context, JwtConfig jwtConfig)
    {
        _context = context;
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
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            UserType = request.UserType
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        var token = GenerateToken(user);
        
        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            UserType = user.UserType,
            Token = token,
            TokenExpiration = DateTime.UtcNow.AddMinutes(_jwtConfig.TokenLifetimeMinutes)
        };
    }
    
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);
            
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password");
        }
        
        var token = GenerateToken(user);
        
        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            UserType = user.UserType,
            Token = token,
            TokenExpiration = DateTime.UtcNow.AddMinutes(_jwtConfig.TokenLifetimeMinutes)
        };
    }
    
    public async Task<bool> IsEmailUniqueAsync(string email)
    {
        return !await _context.Users.AnyAsync(u => u.Email == email);
    }
    
    public string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
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