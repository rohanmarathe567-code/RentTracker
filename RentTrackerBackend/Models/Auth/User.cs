using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RentTrackerBackend.Models.Auth;

public class User : BaseDocument
{
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;
    
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;
    
    [BsonElement("userType")]
    public UserType UserType { get; set; }
}