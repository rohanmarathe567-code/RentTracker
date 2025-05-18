using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
            IPaymentMethodRepository repository,
            IClaimsPrincipalService claimsPrincipalService,
            ILogger<PaymentMethod> logger) =>
        {
            try 
            {
                logger.LogInformation("GET request received for payment methods");
                
                // Validate tenant ID but always return system payment methods
                bool hasTenantId = claimsPrincipalService.ValidateTenantId(out string tenantId);
                if (!hasTenantId)
                {
                    logger.LogWarning("No valid tenant ID found in request, returning only system payment methods");
                    // Even without tenant authentication, we can still provide system payment methods
                    var onlySystemDefaults = await repository.GetAllAsync("system");
                    return Results.Ok(onlySystemDefaults.OrderBy(p => p.Name).ToList());
                }
                
                logger.LogInformation($"Fetching all payment methods for authenticated user");

                // Get all payment methods since user is authenticated
                var allMethods = await repository.GetAllSharedAsync();
                var paymentMethods = allMethods.OrderBy(p => p.Name).ToList();
                
                logger.LogInformation($"Returning {paymentMethods.Count} payment methods");
                return Results.Ok(paymentMethods);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing payment methods request");
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        })
        .WithName("GetPaymentMethods")
        .Produces<IEnumerable<PaymentMethod>>(StatusCodes.Status200OK)
        .RequireAuthorization();  // Require authentication

        // Create a new payment method
        app.MapPost("/api/paymentmethods", async (
            IPaymentMethodRepository repository,
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
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization();  // Require authentication
    }
}
