using MongoDB.Driver;
using RentTrackerBackend.Data;

namespace RentTrackerBackend.Endpoints;

public static class HealthController
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", async (IMongoClient mongoClient) =>
        {
            try
            {
                await mongoClient.ListDatabaseNamesAsync();
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
                    }!
                );
            }
        });
    }
}