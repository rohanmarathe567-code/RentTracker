using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Models.Pagination;
using RentTrackerBackend.Extensions;

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
                query = query.Where(p => 
                    p.Address.Contains(parameters.SearchTerm) || 
                    (p.Suburb != null && p.Suburb.Contains(parameters.SearchTerm)));
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
        app.MapPost("/api/properties", async (RentalProperty property, ApplicationDbContext db) =>
        {
            property.CreatedAt = DateTime.UtcNow;
            property.UpdatedAt = DateTime.UtcNow;
            
            db.RentalProperties.Add(property);
            await db.SaveChangesAsync();
            
            return Results.Created($"/api/properties/{property.Id}", property);
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