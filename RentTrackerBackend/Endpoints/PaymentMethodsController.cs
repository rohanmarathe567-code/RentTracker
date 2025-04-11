using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using System.Security.Claims;

namespace RentTrackerBackend.Endpoints;

public static class PaymentMethodsController
{
    public static void MapPaymentMethodEndpoints(this WebApplication app)
    {
        // Get all payment methods
        app.MapGet("/api/paymentmethods", async (ApplicationDbContext context, ClaimsPrincipal user) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Results.Unauthorized();

            var paymentMethods = await context.PaymentMethods
                .Where(p => p.IsSystemDefault || p.UserId == userId)
                .OrderBy(p => p.Name)
                .ToListAsync();
                
            return Results.Ok(paymentMethods);
        })
        .WithName("GetPaymentMethods")
        .Produces<IEnumerable<PaymentMethod>>(StatusCodes.Status200OK);

        // Create a new payment method
        app.MapPost("/api/paymentmethods", async (
            ApplicationDbContext context,
            PaymentMethod paymentMethod,
            ClaimsPrincipal user) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Results.Unauthorized();

            // Only admins can create system-wide payment methods
            if (paymentMethod.IsSystemDefault && !user.IsInRole("Admin"))
                return Results.Forbid();

            paymentMethod.UserId = paymentMethod.IsSystemDefault ? null : userId;

            paymentMethod.CreatedAt = DateTime.UtcNow;
            paymentMethod.UpdatedAt = DateTime.UtcNow;

            context.PaymentMethods.Add(paymentMethod);
            await context.SaveChangesAsync();

            return Results.CreatedAtRoute(
                "GetPaymentMethods",
                new { id = paymentMethod.Id },
                paymentMethod
            );
        })
        .WithName("CreatePaymentMethod")
        .Produces<PaymentMethod>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);
    }

}
