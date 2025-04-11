using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Models;
using RentTrackerBackend.Models.Pagination;
using RentTrackerBackend.Extensions;
using RentTrackerBackend.Services;
using RentTrackerBackend.Data;
using System.Security.Claims;

namespace RentTrackerBackend.Endpoints;

public static class PaymentsController
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        // Get paginated payments for a specific property
        app.MapGet("/api/properties/{propertyId}/payments", async (
            Guid propertyId,
            [AsParameters] PaginationParameters parameters,
            IPaymentService paymentService,
            ApplicationDbContext db,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                    return Results.Unauthorized();

                // Verify property ownership
                var property = await db.RentalProperties.FindAsync(propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                if (property.UserId != userId)
                    return Results.Forbid();

                var query = await paymentService.GetPaymentsByPropertyQueryAsync(propertyId);
                
                // Optional: Add search filtering if needed
                if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
                {
                    query = query.Where(p =>
                        (p.PaymentMethod != null && p.PaymentMethod.Name.Contains(parameters.SearchTerm)) ||
                        (p.PaymentReference != null && p.PaymentReference.Contains(parameters.SearchTerm)) ||
                        (p.Notes != null && p.Notes.Contains(parameters.SearchTerm)));
                }

                // Apply sorting if specified
                if (!string.IsNullOrWhiteSpace(parameters.SortField))
                {
                    query = parameters.SortField.ToLower() switch
                    {
                        "paymentdate" => parameters.SortDescending
                            ? query.OrderByDescending(p => p.PaymentDate)
                            : query.OrderBy(p => p.PaymentDate),
                        "amount" => parameters.SortDescending
                            ? query.OrderByDescending(p => p.Amount)
                            : query.OrderBy(p => p.Amount),
                        "paymentmethod" => parameters.SortDescending
                            ? query.OrderByDescending(p => p.PaymentMethod!.Name)
                            : query.OrderBy(p => p.PaymentMethod!.Name),
                        "paymentreference" => parameters.SortDescending
                            ? query.OrderByDescending(p => p.PaymentReference)
                            : query.OrderBy(p => p.PaymentReference),
                        "notes" => parameters.SortDescending
                            ? query.OrderByDescending(p => p.Notes)
                            : query.OrderBy(p => p.Notes),
                        _ => query.OrderByDescending(p => p.PaymentDate) // Default sort by payment date descending
                    };
                }
                
                var payments = await query.ToPaginatedListAsync(parameters);
                
                return Results.Ok(payments);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        });

        // Get a specific payment by ID for a specific property
        app.MapGet("/api/properties/{propertyId}/payments/{paymentId}", async (
            Guid propertyId,
            Guid paymentId,
            IPaymentService paymentService,
            ApplicationDbContext db,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                    return Results.Unauthorized();

                // Verify property ownership
                var property = await db.RentalProperties.FindAsync(propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                if (property.UserId != userId)
                    return Results.Forbid();

                var payment = await paymentService.GetPaymentByIdAsync(paymentId);
                
                // Additional validation to ensure payment belongs to the specified property
                if (payment == null || payment.RentalPropertyId != propertyId)
                {
                    return Results.NotFound($"Payment with ID {paymentId} not found for property {propertyId}");
                }
                
                return Results.Ok(payment);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        });

        // Update an existing payment for a specific property
        app.MapPut("/api/properties/{propertyId}/payments/{paymentId}", async (
            Guid propertyId,
            Guid paymentId,
            RentalPayment updatedPayment,
            IPaymentService paymentService,
            ApplicationDbContext db,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                    return Results.Unauthorized();

                // Verify property ownership
                var property = await db.RentalProperties.FindAsync(propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                if (property.UserId != userId)
                    return Results.Forbid();

                // Validate that the payment belongs to the specified property
                var existingPayment = await paymentService.GetPaymentByIdAsync(paymentId);
                if (existingPayment == null || existingPayment.RentalPropertyId != propertyId)
                {
                    return Results.NotFound($"Payment with ID {paymentId} not found for property {propertyId}");
                }

                // Ensure the updated payment is for the correct property
                updatedPayment.RentalPropertyId = propertyId;

                var payment = await paymentService.UpdatePaymentAsync(paymentId, updatedPayment);
                return payment != null
                    ? Results.NoContent()
                    : Results.NotFound($"Payment with ID {paymentId} not found");
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        });

        // Delete a payment for a specific property
        app.MapDelete("/api/properties/{propertyId}/payments/{paymentId}", async (
            Guid propertyId,
            Guid paymentId,
            IPaymentService paymentService,
            ApplicationDbContext db,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                    return Results.Unauthorized();

                // Verify property ownership
                var property = await db.RentalProperties.FindAsync(propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                if (property.UserId != userId)
                    return Results.Forbid();

                // Validate that the payment belongs to the specified property
                var existingPayment = await paymentService.GetPaymentByIdAsync(paymentId);
                if (existingPayment == null || existingPayment.RentalPropertyId != propertyId)
                {
                    return Results.NotFound($"Payment with ID {paymentId} not found for property {propertyId}");
                }

                var deleted = await paymentService.DeletePaymentAsync(paymentId);
                return deleted
                    ? Results.NoContent()
                    : Results.NotFound($"Payment with ID {paymentId} not found");
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        });

        // Create a new payment for a specific property
        app.MapPost("/api/properties/{propertyId}/payments", async (
            Guid propertyId,
            RentalPayment payment,
            IPaymentService paymentService,
            ApplicationDbContext db,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                    return Results.Unauthorized();

                // Verify property ownership
                var property = await db.RentalProperties.FindAsync(propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                if (property.UserId != userId)
                    return Results.Forbid();

                // Ensure the payment is associated with the correct property
                payment.RentalPropertyId = propertyId;
                
                var createdPayment = await paymentService.CreatePaymentAsync(payment);
                
                return Results.Created($"/api/properties/{propertyId}/payments/{createdPayment.Id}", createdPayment);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        });
    }
}