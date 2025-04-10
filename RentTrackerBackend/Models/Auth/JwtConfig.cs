namespace RentTrackerBackend.Models.Auth;

public class JwtConfig
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int TokenLifetimeMinutes { get; set; } = 60;
    
    public const string SectionName = "Jwt";
}