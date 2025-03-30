using Microsoft.EntityFrameworkCore;
using RentTracker.Data;
using RentTracker.Models;

namespace RentTracker.Endpoints;

public static class PaymentsController
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/properties/{propertyId}/payments", async (int propertyId, ApplicationDbContext db) =>
            await db.RentalPayments
                .Where(p => p.RentalPropertyId == propertyId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync());

        app.MapGet("/api/payments/{id}", async (int id, ApplicationDbContext db) =>
            await db.RentalPayments.FindAsync(id) is { } payment
                ? Results.Ok(payment)
                : Results.NotFound());

        app.MapPost("/api/payments", async (RentalPayment payment, ApplicationDbContext db) =>
        {
            payment.CreatedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;
            
            db.RentalPayments.Add(payment);
            await db.SaveChangesAsync();
            
            return Results.Created($"/api/payments/{payment.Id}", payment);
        });

        app.MapPut("/api/payments/{id}", async (int id, RentalPayment updatedPayment, ApplicationDbContext db) =>
        {
            var payment = await db.RentalPayments.FindAsync(id);
            
            if (payment == null)
                return Results.NotFound();
            
            payment.Amount = updatedPayment.Amount;
            payment.PaymentDate = updatedPayment.PaymentDate;
            payment.PaymentMethod = updatedPayment.PaymentMethod;
            payment.PaymentReference = updatedPayment.PaymentReference;
            payment.Notes = updatedPayment.Notes;
            payment.UpdatedAt = DateTime.UtcNow;
            
            await db.SaveChangesAsync();
            
            return Results.NoContent();
        });

        app.MapDelete("/api/payments/{id}", async (int id, ApplicationDbContext db) =>
        {
            var payment = await db.RentalPayments.FindAsync(id);
            
            if (payment == null)
                return Results.NotFound();
            
            db.RentalPayments.Remove(payment);
            await db.SaveChangesAsync();
            
            return Results.NoContent();
        });
    }
}