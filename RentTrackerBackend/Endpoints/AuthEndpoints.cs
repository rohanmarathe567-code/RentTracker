using RentTrackerBackend.Models.Auth;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");
        
        // Register new user
        group.MapPost("/register", async (
            RegisterRequest request,
            IAuthService authService) =>
        {
            try
            {
                var response = await authService.RegisterAsync(request);
                return Results.Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("Register")
        .WithOpenApi();
        
        // Login
        group.MapPost("/login", async (
            LoginRequest request,
            IAuthService authService) =>
        {
            try
            {
                var response = await authService.LoginAsync(request);
                return Results.Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("Login")
        .WithOpenApi();
        
        // Test protected endpoint for admin
        group.MapGet("/admin-test", () => Results.Ok(new { message = "Admin access successful" }))
            .RequireAuthorization("RequireAdminRole")
            .WithName("AdminTest")
            .WithOpenApi();
            
        // Test protected endpoint for users
        group.MapGet("/user-test", () => Results.Ok(new { message = "User access successful" }))
            .RequireAuthorization("RequireUserRole")
            .WithName("UserTest")
            .WithOpenApi();
    }
}