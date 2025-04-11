using System.Text.Json.Serialization;

namespace RentTrackerBackend.Models.Auth;

/// <summary>
/// Represents the type of user in the system.
/// Valid values: "Admin", "User" (case-sensitive)
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserType
{
    /// <summary>
    /// Administrator with full system access
    /// </summary>
    Admin,
    
    /// <summary>
    /// Regular user with standard permissions
    /// </summary>
    User
}