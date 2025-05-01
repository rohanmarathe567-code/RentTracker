using RentTrackerBackend.Models;
using RentTrackerBackend.Models.Pagination;
using RentTrackerBackend.Services;
using RentTrackerBackend.Data;
using MongoDB.Driver;

namespace RentTrackerBackend.Endpoints;

public static class PaymentsController
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        // Get paginated payments for a specific property
        app.MapGet("/api/properties/{propertyId}/payments", async (
            string propertyId,
            [AsParameters] PaginationParameters parameters,
            IPaymentService paymentService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                // Parse the include parameter from query string
                string[]? includes = null;
                if (parameters.Include != null)
                {
                    includes = parameters.Include.Split(',', StringSplitOptions.RemoveEmptyEntries);
                }

                var payments = await paymentService.GetPaymentsByPropertyAsync(tenantId, propertyId, includeSystem: true, includes: includes);
                
                // Manual filtering
                var filteredPayments = payments.AsQueryable();
                if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
                {
                    filteredPayments = filteredPayments.Where(p =>
                        (p.PaymentMethod != null && p.PaymentMethod.Name.Contains(parameters.SearchTerm)) ||
                        (p.PaymentReference != null && p.PaymentReference.Contains(parameters.SearchTerm)) ||
                        (p.Notes != null && p.Notes.Contains(parameters.SearchTerm)));
                }

                // Apply sorting
                if (!string.IsNullOrWhiteSpace(parameters.SortField))
                {
                    filteredPayments = parameters.SortField.ToLower() switch
                    {
                        "paymentdate" => parameters.SortDescending
                            ? filteredPayments.OrderByDescending(p => p.PaymentDate)
                            : filteredPayments.OrderBy(p => p.PaymentDate),
                        "amount" => parameters.SortDescending
                            ? filteredPayments.OrderByDescending(p => p.Amount)
                            : filteredPayments.OrderBy(p => p.Amount),
                        "paymentmethod" => parameters.SortDescending
                            ? filteredPayments.OrderByDescending(p => p.PaymentMethod!.Name)
                            : filteredPayments.OrderBy(p => p.PaymentMethod!.Name),
                        "paymentreference" => parameters.SortDescending
                            ? filteredPayments.OrderByDescending(p => p.PaymentReference)
                            : filteredPayments.OrderBy(p => p.PaymentReference),
                        "notes" => parameters.SortDescending
                            ? filteredPayments.OrderByDescending(p => p.Notes)
                            : filteredPayments.OrderBy(p => p.Notes),
                        _ => filteredPayments.OrderByDescending(p => p.PaymentDate)
                    };
                }

                // Manual pagination
                var totalCount = filteredPayments.Count();
                var items = filteredPayments
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToList();

                var result = PaginatedResponse<RentalPayment>.Create(
                    items,
                    totalCount,
                    parameters.PageNumber,
                    parameters.PageSize);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        });

        // Get a specific payment by ID for a specific property
        app.MapGet("/api/properties/{propertyId}/payments/{paymentId}", async (
            string propertyId,
            string paymentId,
            IPaymentService paymentService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                var payment = await paymentService.GetPaymentByIdAsync(tenantId, paymentId);
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
            string propertyId,
            string paymentId,
            RentalPayment updatedPayment,
            IPaymentService paymentService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                var existingPayment = await paymentService.GetPaymentByIdAsync(tenantId, paymentId);
                if (existingPayment == null || existingPayment.RentalPropertyId != propertyId)
                {
                    return Results.NotFound($"Payment with ID {paymentId} not found for property {propertyId}");
                }

                updatedPayment.RentalPropertyId = propertyId;
                updatedPayment.TenantId = tenantId;

                var payment = await paymentService.UpdatePaymentAsync(tenantId, paymentId, updatedPayment);
                return payment != null
                    ? Results.NoContent()
                    : Results.NotFound($"Payment with ID {paymentId} not found");
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        });

        // Delete a payment for a specific property
        app.MapDelete("/api/properties/{propertyId}/payments/{paymentId}", async (
            string propertyId,
            string paymentId,
            IPaymentService paymentService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                var existingPayment = await paymentService.GetPaymentByIdAsync(tenantId, paymentId);
                if (existingPayment == null || existingPayment.RentalPropertyId != propertyId)
                {
                    return Results.NotFound($"Payment with ID {paymentId} not found for property {propertyId}");
                }

                var deleted = await paymentService.DeletePaymentAsync(tenantId, paymentId);
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
            string propertyId,
            RentalPayment payment,
            IPaymentService paymentService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                payment.RentalPropertyId = propertyId;
                payment.TenantId = tenantId;

                var createdPayment = await paymentService.CreatePaymentAsync(payment);
                
                return Results.Created($"/api/properties/{propertyId}/payments/{createdPayment.Id}", createdPayment);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        });
    }
}