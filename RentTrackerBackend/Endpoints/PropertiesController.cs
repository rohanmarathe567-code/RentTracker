using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Models.Pagination;
using RentTrackerBackend.Extensions;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Endpoints;

public static class PropertiesController
{
    public static void MapPropertyEndpoints(this WebApplication app)
    {
        // Get paginated list of properties with optional search
        app.MapGet("/api/properties", async (
            [AsParameters] PaginationParameters parameters, 
            ApplicationDbContext db) =>
        {
            var query = db.RentalProperties.AsNoTracking();
            
            // Optional: Add search filtering if needed
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.Address.ToLower().Contains(searchTerm) ||
                    (p.Suburb != null && p.Suburb.ToLower().Contains(searchTerm)) ||
                    (p.State != null && p.State.ToLower().Contains(searchTerm)) ||
                    (p.PostCode != null && p.PostCode.Contains(searchTerm)) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                    (p.PropertyManager != null && p.PropertyManager.ToLower().Contains(searchTerm)) ||
                    (p.PropertyManagerContact != null && p.PropertyManagerContact.ToLower().Contains(searchTerm)));
            }

            // Apply sorting if specified
            if (!string.IsNullOrWhiteSpace(parameters.SortField))
            {
                query = parameters.SortField.ToLower() switch
                {
                    "address" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.Address)
                        : query.OrderBy(p => p.Address),
                    "suburb" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.Suburb)
                        : query.OrderBy(p => p.Suburb),
                    "state" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.State)
                        : query.OrderBy(p => p.State),
                    "weeklyrentamount" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.WeeklyRentAmount)
                        : query.OrderBy(p => p.WeeklyRentAmount),
                    "leasestartdate" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.LeaseStartDate)
                        : query.OrderBy(p => p.LeaseStartDate),
                    "leaseenddate" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.LeaseEndDate)
                        : query.OrderBy(p => p.LeaseEndDate),
                    _ => query.OrderBy(p => p.Address) // Default sort
                };
            }
            
            var result = await query.ToPaginatedListAsync(parameters);
            
            return Results.Ok(result);
        });

        // Get a specific property by ID
        app.MapGet("/api/properties/{id}", async (Guid id, ApplicationDbContext db) =>
            await db.RentalProperties.FindAsync(id) is RentalProperty property
                ? Results.Ok(property)
                : Results.NotFound());

        // Create a new property
        app.MapPost("/api/properties", async (RentalProperty property, ApplicationDbContext db, ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("Creating new property: {@Property}", property);
                
                // Ensure required fields are set
                if (string.IsNullOrWhiteSpace(property.Address))
                {
                    logger.LogWarning("Property creation failed: Address is required");
                    return Results.BadRequest("Address is required");
                }
                
                // Set default values for CreatedAt and UpdatedAt
                property.CreatedAt = DateTime.UtcNow;
                property.UpdatedAt = DateTime.UtcNow;
                
                // Always generate a new ID for new properties
                property.Id = SequentialGuidGenerator.NewSequentialGuid();
                
                // No need to initialize navigation properties as they've been removed
                
                logger.LogDebug("Adding property to database: {@Property}", property);
                db.RentalProperties.Add(property);
                
                await db.SaveChangesAsync();
                logger.LogInformation("Property created successfully with ID: {Id}", property.Id);
                
                return Results.Created($"/api/properties/{property.Id}", property);
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
        });

        // Update an existing property
        app.MapPut("/api/properties/{id}", async (Guid id, RentalProperty updatedProperty, ApplicationDbContext db) =>
        {
            var property = await db.RentalProperties.FindAsync(id);
            
            if (property == null)
                return Results.NotFound();
            
            property.Address = updatedProperty.Address;
            property.Suburb = updatedProperty.Suburb;
            property.State = updatedProperty.State;
            property.PostCode = updatedProperty.PostCode;
            property.Description = updatedProperty.Description;
            property.WeeklyRentAmount = updatedProperty.WeeklyRentAmount;
            property.LeaseStartDate = updatedProperty.LeaseStartDate;
            property.LeaseEndDate = updatedProperty.LeaseEndDate;
            property.PropertyManager = updatedProperty.PropertyManager;
            property.PropertyManagerContact = updatedProperty.PropertyManagerContact;
            property.UpdatedAt = DateTime.UtcNow;
            
            await db.SaveChangesAsync();
            
            return Results.Ok(property);
        });

        // Delete a property
        app.MapDelete("/api/properties/{id}", async (Guid id, ApplicationDbContext db) =>
        {
            var property = await db.RentalProperties.FindAsync(id);
            
            if (property == null)
                return Results.NotFound();
            
            db.RentalProperties.Remove(property);
            await db.SaveChangesAsync();
            
            return Results.NoContent();
        });
    }
}