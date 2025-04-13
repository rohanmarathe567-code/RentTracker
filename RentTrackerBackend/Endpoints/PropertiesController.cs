using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Models.Pagination;
using RentTrackerBackend.Extensions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;

namespace RentTrackerBackend.Endpoints;

public static class PropertiesController
{
    public static void MapPropertyEndpoints(this WebApplication app)
    {
        // Get paginated list of properties with optional search
        app.MapGet("/api/properties", async (
            [AsParameters] PaginationParameters parameters,
            IMongoRepository<RentalProperty> propertyRepository,
            ClaimsPrincipal user,
            ILogger<Program> logger) =>
        {
            try
            {
                parameters.Validate();
            }
            catch (Exception ex)
            {
                logger.LogWarning("Invalid pagination parameters: {Message}", ex.Message);
                return Results.BadRequest($"Invalid pagination parameters: {ex.Message}");
            }
            
            logger.LogInformation("GET /api/properties called with parameters: PageNumber={PageNumber}, PageSize={PageSize}",
                parameters.PageNumber, parameters.PageSize);

            var tenantId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(tenantId))
            {
                logger.LogWarning("User ID claim not found");
                return Results.Unauthorized();
            }

            var properties = await propertyRepository.GetAllAsync(tenantId);
            var query = properties.AsQueryable();
            
            // Optional: Add search filtering if needed
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.Address.Street.ToLower().Contains(searchTerm) ||
                    p.Address.City.ToLower().Contains(searchTerm) ||
                    p.Address.State.ToLower().Contains(searchTerm) ||
                    p.Address.ZipCode.Contains(searchTerm) ||
                    p.PropertyManager.Name.ToLower().Contains(searchTerm) ||
                    p.PropertyManager.Contact.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm));
            }

            // Apply sorting if specified
            if (!string.IsNullOrWhiteSpace(parameters.SortField))
            {
                query = parameters.SortField.ToLower() switch
                {
                    "address" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.Address.Street)
                        : query.OrderBy(p => p.Address.Street),
                    "city" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.Address.City)
                        : query.OrderBy(p => p.Address.City),
                    "state" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.Address.State)
                        : query.OrderBy(p => p.Address.State),
                    "rentamount" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.RentAmount)
                        : query.OrderBy(p => p.RentAmount),
                    "leasestartdate" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.LeaseDates.StartDate)
                        : query.OrderBy(p => p.LeaseDates.StartDate),
                    "leaseenddate" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.LeaseDates.EndDate)
                        : query.OrderBy(p => p.LeaseDates.EndDate),
                    _ => query.OrderBy(p => p.Address.Street) // Default sort
                };
            }
            
            var result = query.ToPaginatedList(parameters);
            
            return Results.Ok(result);
        }).RequireAuthorization();

        // Get a specific property by ID
        app.MapGet("/api/properties/{id}", async (
            string id,
            IMongoRepository<RentalProperty> propertyRepository,
            ClaimsPrincipal user) =>
        {
            var tenantId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(tenantId))
                return Results.Unauthorized();

            var property = await propertyRepository.GetByIdAsync(tenantId, id);
            return property != null ? Results.Ok(property) : Results.NotFound();
        }).RequireAuthorization();

        // Create a new property
        app.MapPost("/api/properties", async (
            RentalProperty property,
            IMongoRepository<RentalProperty> propertyRepository,
            ILogger<Program> logger,
            ClaimsPrincipal user) =>
        {
            var tenantId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(tenantId))
                return Results.Unauthorized();

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
                
                logger.LogInformation("Creating new property: {@Property}", newProperty);
                
                // Ensure required fields are set
                if (string.IsNullOrWhiteSpace(property.Address.Street))
                {
                    logger.LogWarning("Property creation failed: Address is required");
                    return Results.BadRequest("Address is required");
                }

                var createdProperty = await propertyRepository.CreateAsync(newProperty);
                logger.LogInformation("Property created successfully with ID: {Id}", createdProperty.FormattedId);
                
                return Results.Created($"/api/properties/{createdProperty.Id}", createdProperty);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating property: {Message}", ex.Message);
                return Results.Problem(
                    title: "Error creating property",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        }).RequireAuthorization();

        // Update an existing property
        app.MapPut("/api/properties/{id}", async (
            string id,
            RentalProperty updatedProperty,
            IMongoRepository<RentalProperty> propertyRepository,
            ClaimsPrincipal user) =>
        {
            var tenantId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(tenantId))
                return Results.Unauthorized();

            var property = await propertyRepository.GetByIdAsync(tenantId, id);
            
            if (property == null)
                return Results.NotFound();

            // Create new property instance with existing ID
            var propertyToUpdate = new RentalProperty
            {
                Id = property.Id,
                TenantId = tenantId,
                CreatedAt = property.CreatedAt,          // Preserve original creation time
                UpdatedAt = DateTime.UtcNow,             // Set new update time
                Version = property.Version,              // Use current version from DB, not client
                Address = updatedProperty.Address,
                Description = updatedProperty.Description,
                RentAmount = updatedProperty.RentAmount,
                LeaseDates = updatedProperty.LeaseDates,
                PropertyManager = updatedProperty.PropertyManager,
                Attributes = updatedProperty.Attributes,
                PaymentIds = property.PaymentIds,        // Preserve existing payment references
                AttachmentIds = property.AttachmentIds  // Preserve existing attachment references
            };
            
            await propertyRepository.UpdateAsync(tenantId, id, propertyToUpdate);
            
            return Results.Ok(propertyToUpdate);
        }).RequireAuthorization();

        // Delete a property
        app.MapDelete("/api/properties/{id}", async (
            string id,
            IMongoRepository<RentalProperty> propertyRepository,
            ClaimsPrincipal user) =>
        {
            var tenantId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(tenantId))
                return Results.Unauthorized();

            var property = await propertyRepository.GetByIdAsync(tenantId, id);
            
            if (property == null)
                return Results.NotFound();
            
            await propertyRepository.DeleteAsync(tenantId, id);
            
            return Results.NoContent();
        }).RequireAuthorization();
    }
}