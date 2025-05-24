using RentTrackerBackend.Models;
using RentTrackerBackend.Models.Pagination;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Endpoints;

public static class PropertiesController
{
    public static void MapPropertyEndpoints(this WebApplication app)
    {
        // Get paginated list of properties with optional search
        app.MapGet("/api/properties", async (
            [AsParameters] PaginationParameters parameters,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService,
            ILogger<Program> logger) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                var result = await propertyService.GetPropertiesAsync(tenantId, parameters);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning("Invalid pagination parameters: {Message}", ex.Message);
                return Results.BadRequest($"Invalid pagination parameters: {ex.Message}");
            }
        }).RequireAuthorization();

        // Get a specific property by ID
        app.MapGet("/api/properties/{id}", async (
            string id,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            var property = await propertyService.GetPropertyByIdAsync(tenantId, id);
            return property != null ? Results.Ok(property) : Results.NotFound();
        }).RequireAuthorization();

        // Create a new property
        app.MapPost("/api/properties", async (
            RentalProperty property,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService,
            ILogger<Program> logger) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            try
            {
                // Create a new property instance to ensure we don't use any client-provided IDs
                var newProperty = new RentalProperty
                {
                    TenantId = tenantId,
                    Address = property.Address,
                    Description = property.Description,
                    RentAmount = property.RentAmount,
                    LeaseDates = property.LeaseDates,
                    PropertyManager = property.PropertyManager
                };

                var createdProperty = await propertyService.CreatePropertyAsync(newProperty);
                return Results.Created($"/api/properties/{createdProperty.Id}", createdProperty);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create property");
                return Results.Problem(
                    title: "Error creating property",
                    detail: "An unexpected error occurred",
                    statusCode: 500
                );
            }
        }).RequireAuthorization();

        // Update an existing property
        app.MapPut("/api/properties/{id}", async (
            string id,
            RentalProperty updatedProperty,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            var result = await propertyService.UpdatePropertyAsync(tenantId, id, updatedProperty);
            return result != null ? Results.Ok(result) : Results.NotFound();
        }).RequireAuthorization();

        // Delete a property
        app.MapDelete("/api/properties/{id}", async (
            string id,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            var result = await propertyService.DeletePropertyAsync(tenantId, id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();
    }
}