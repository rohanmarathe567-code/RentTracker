using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RentTrackerBackend.Models.Auth;

public class User : BaseDocument
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserType UserType { get; set; }
}