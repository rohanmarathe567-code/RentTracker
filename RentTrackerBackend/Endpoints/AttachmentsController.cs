using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Services;
using RentTrackerBackend.Endpoints;
using System;

namespace RentTrackerBackend.Endpoints;

public static class AttachmentsController
{
    public static void MapAttachmentEndpoints(this WebApplication app)
    {
        // Property Attachments
        app.MapGet("/api/properties/{propertyId}/attachments", async (Guid propertyId, ApplicationDbContext db) =>
        {
            var attachments = await db.Attachments
                .Where(a => a.RentalPropertyId == propertyId)
                .ToListAsync();
            return Results.Ok(attachments);
        });

        app.MapPost("/api/properties/{propertyId}/attachments", async (
            Guid propertyId,
            IFormFile file,
            ApplicationDbContext db,
            IWebHostEnvironment env,
            IAttachmentService attachmentService) =>
        {
            // Validate property exists
            var property = await db.RentalProperties.FindAsync(propertyId);
            if (property == null)
                return Results.NotFound($"Property with ID {propertyId} not found.");

            // Validate file
            if (file == null || file.Length == 0)
                return Results.BadRequest("No file uploaded.");

            try
            {
                var attachment = await attachmentService.SaveAttachmentAsync(
                    file,
                    RentalAttachmentType.Property,
                    propertyId);

                return Results.Created($"/api/attachments/{attachment.Id}", attachment);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"File upload failed: {ex.Message}");
            }
        });

        // Payment Attachments
        app.MapGet("/api/properties/{propertyId}/payments/{paymentId}/attachments", async (
            Guid propertyId,
            Guid paymentId,
            ApplicationDbContext db) =>
        {
            var attachments = await db.Attachments
                .Where(a => a.RentalPaymentId == paymentId)
                .ToListAsync();
            return Results.Ok(attachments);
        });

        app.MapPost("/api/properties/{propertyId}/payments/{paymentId}/attachments", async (
            Guid propertyId,
            Guid paymentId,
            IFormFile file,
            ApplicationDbContext db,
            IWebHostEnvironment env,
            IAttachmentService attachmentService) =>
        {
            // Validate payment exists and belongs to property
            var payment = await db.RentalPayments
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.RentalPropertyId == propertyId);
            
            if (payment == null)
                return Results.NotFound($"Payment with ID {paymentId} for Property {propertyId} not found.");

            // Validate file
            if (file == null || file.Length == 0)
                return Results.BadRequest("No file uploaded.");

            try
            {
                var attachment = await attachmentService.SaveAttachmentAsync(
                    file,
                    RentalAttachmentType.Payment,
                    paymentId);

                return Results.Created($"/api/attachments/{attachment.Id}", attachment);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"File upload failed: {ex.Message}");
            }
        });

        // Generic Attachment Endpoints
        app.MapGet("/api/attachments/{attachmentId}", async (
            Guid attachmentId,
            ApplicationDbContext db,
            IAttachmentService attachmentService) =>
        {
            var attachment = await db.Attachments.FindAsync(attachmentId);
            
            if (attachment == null)
                return Results.NotFound($"Attachment with ID {attachmentId} not found.");

            return Results.Ok(attachment);
        });
    }
}

// Enum to specify attachment type
public enum RentalAttachmentType
{
    Property,
    Payment
}