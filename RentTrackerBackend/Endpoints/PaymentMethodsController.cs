using Microsoft.AspNetCore.Mvc;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Services;
using MongoDB.Driver;

namespace RentTrackerBackend.Endpoints;

public static class PaymentMethodsController
{
    public static void MapPaymentMethodEndpoints(this WebApplication app)
    {
        // Get all payment methods
        app.MapGet("/api/paymentmethods", async (
            IMongoRepository<PaymentMethod> repository,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            // Get system defaults and user's custom payment methods
            var systemDefaults = await repository.GetAllAsync("system");
            var userMethods = await repository.GetAllAsync(tenantId);

            var paymentMethods = systemDefaults.Union(userMethods)
                .OrderBy(p => p.Name)
                .ToList();
                
            return Results.Ok(paymentMethods);
        })
        .WithName("GetPaymentMethods")
        .Produces<IEnumerable<PaymentMethod>>(StatusCodes.Status200OK);

        // Create a new payment method
        app.MapPost("/api/paymentmethods", async (
            IMongoRepository<PaymentMethod> repository,
            IClaimsPrincipalService claimsPrincipalService,
            PaymentMethod paymentMethod) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            // Only admins can create system-wide payment methods
            if (paymentMethod.IsSystemDefault && !claimsPrincipalService.IsInRole("Admin"))
                return Results.Forbid();

            // Set the appropriate tenant ID based on whether it's a system default
            paymentMethod.TenantId = paymentMethod.IsSystemDefault ? "system" : tenantId;
            paymentMethod.UserId = tenantId;

            var createdMethod = await repository.CreateAsync(paymentMethod);

            return Results.CreatedAtRoute(
                "GetPaymentMethods",
                new { id = createdMethod.Id.ToString() },
                createdMethod
            );
        })
        .WithName("CreatePaymentMethod")
        .Produces<PaymentMethod>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
