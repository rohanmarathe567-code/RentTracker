using RentTracker.Data;

namespace RentTracker.Api.Endpoints;

public static class HealthController
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", async (ApplicationDbContext db) =>
        {
            try
            {
                await db.Database.CanConnectAsync();
                return Results.Ok(new { Status = "Healthy", Database = "Connected" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Database Connection Failed",
                    extensions: new Dictionary<string, object>
                    {
                        { "Status", "Unhealthy" },
                        { "Database", "Disconnected" }
                    }
                );
            }
        });
    }
}