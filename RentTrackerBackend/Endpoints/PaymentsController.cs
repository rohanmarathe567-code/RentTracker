using System;
using Microsoft.AspNetCore.Mvc;
using RentTrackerBackend.Models;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Endpoints;

public static class PaymentsController
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        // Get payments for a specific property
        app.MapGet("/api/properties/{propertyId}/payments", async (Guid propertyId, IPaymentService paymentService) =>
        {
            try
            {
                var payments = await paymentService.GetPaymentsByPropertyIdAsync(propertyId);
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
        app.MapGet("/api/properties/{propertyId}/payments/{paymentId}", async (Guid propertyId, Guid paymentId, IPaymentService paymentService) =>
        {
            try
            {
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
        app.MapPut("/api/properties/{propertyId}/payments/{paymentId}", async (Guid propertyId, Guid paymentId, RentalPayment updatedPayment, IPaymentService paymentService) =>
        {
            try
            {
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
        app.MapDelete("/api/properties/{propertyId}/payments/{paymentId}", async (Guid propertyId, Guid paymentId, IPaymentService paymentService) =>
        {
            try
            {
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
    }
}