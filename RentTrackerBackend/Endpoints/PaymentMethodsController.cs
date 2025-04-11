using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Endpoints;

public static class PaymentMethodsController
{
    public static void MapPaymentMethodEndpoints(this WebApplication app)
    {
        // Get all payment methods
        app.MapGet("/api/paymentmethods", async (ApplicationDbContext context) =>
        {
            var paymentMethods = await context.PaymentMethods
                .OrderBy(p => p.Name)
                .ToListAsync();
                
            return Results.Ok(paymentMethods);
        })
        .WithName("GetPaymentMethods")
        .Produces<IEnumerable<PaymentMethod>>(StatusCodes.Status200OK);

        // Create a new payment method
        app.MapPost("/api/paymentmethods", async (ApplicationDbContext context, PaymentMethod paymentMethod) =>
        {
            //TODO: This is commented out because it is not working as expected. Uncomment and fix it when needed.
            // if (!context.)
            // {
            //     return Results.BadRequest(context.ModelState);
            // }

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
