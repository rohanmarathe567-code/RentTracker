using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Endpoints;

public static class PropertiesController
{
    public static void MapPropertyEndpoints(this WebApplication app)
    {
        app.MapGet("/api/properties", async (ApplicationDbContext db) =>
            await db.RentalProperties.ToListAsync());

        app.MapGet("/api/properties/{id}", async (int id, ApplicationDbContext db) =>
            await db.RentalProperties.FindAsync(id) is RentalProperty property
                ? Results.Ok(property)
                : Results.NotFound());

        app.MapPost("/api/properties", async (RentalProperty property, ApplicationDbContext db) =>
        {
            property.CreatedAt = DateTime.UtcNow;
            property.UpdatedAt = DateTime.UtcNow;
            
            db.RentalProperties.Add(property);
            await db.SaveChangesAsync();
            
            return Results.Created($"/api/properties/{property.Id}", property);
        });

        app.MapPut("/api/properties/{id}", async (int id, RentalProperty updatedProperty, ApplicationDbContext db) =>
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
            
            return Results.NoContent();
        });

        app.MapDelete("/api/properties/{id}", async (int id, ApplicationDbContext db) =>
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