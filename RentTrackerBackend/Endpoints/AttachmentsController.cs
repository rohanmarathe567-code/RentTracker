using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Services;
using RentTrackerBackend.Endpoints;
using System;
using System.Text.Json;

namespace RentTrackerBackend.Endpoints;

public static class AttachmentsController
{
    public static void MapAttachmentEndpoints(this WebApplication app)
    {
        // Download attachment
        app.MapGet("/api/attachments/{attachmentId}/download", async (
            Guid attachmentId,
            ApplicationDbContext db,
            IStorageService storageService) =>
        {
            var attachment = await db.Attachments.FindAsync(attachmentId);
            if (attachment == null)
                return Results.NotFound();

            try
            {
                var stream = await storageService.DownloadFileAsync(attachment.StoragePath);
                return Results.File(stream, attachment.ContentType, attachment.FileName);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"File download failed: {ex.Message}");
            }
        });

        // Delete attachment
        app.MapDelete("/api/attachments/{attachmentId}", async (
            Guid attachmentId,
            ApplicationDbContext db,
            IStorageService storageService) =>
        {
            var attachment = await db.Attachments.FindAsync(attachmentId);
            if (attachment == null)
                return Results.NotFound();

            try
            {
                await storageService.DeleteFileAsync(attachment.StoragePath);
                db.Attachments.Remove(attachment);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"File deletion failed: {ex.Message}");
            }
        });

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
            IStorageService storageService,
            string? description = null,
            string? tags = null) =>
        {
            // Validate property exists
            var property = await db.RentalProperties.FindAsync(propertyId);
            if (property == null)
                return Results.NotFound($"Property with ID {propertyId} not found.");

            // Validate file
            if (file == null || file.Length == 0)
                return Results.BadRequest("No file uploaded.");

            // Check file size (50MB limit)
            if (file.Length > 52428800)
                return Results.BadRequest("File size exceeds 50MB limit.");

            // Validate content type
            if (!storageService.ValidateFileType(file.ContentType, file.FileName))
                return Results.BadRequest($"Invalid file type. Allowed file types: .pdf, .jpg, .jpeg, .png, .gif, .doc, .docx, .xls, .xlsx, .txt");

            try
            {
                // Upload file to storage
                var storagePath = await storageService.UploadFileAsync(file);

                // Create attachment record
                var attachment = new Attachment
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    StoragePath = storagePath,
                    FileSize = file.Length,
                    Description = description,
                    Tags = tags != null ? JsonSerializer.Deserialize<string[]>(tags) : null,
                    EntityType = "Property",
                    RentalPropertyId = propertyId
                };

                db.Attachments.Add(attachment);
                await db.SaveChangesAsync();

                return Results.Created($"/api/attachments/{attachment.Id}", attachment);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"File upload failed: {ex.Message}");
            }
        }).DisableAntiforgery();

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
            IStorageService storageService,
            string? description = null,
            string? tags = null) =>
        {
            // Validate payment exists and belongs to property
            var payment = await db.RentalPayments
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.RentalPropertyId == propertyId);
            
            if (payment == null)
                return Results.NotFound($"Payment with ID {paymentId} for Property {propertyId} not found.");

            // Validate file
            if (file == null || file.Length == 0)
                return Results.BadRequest("No file uploaded.");

            // Check file size (50MB limit)
            if (file.Length > 52428800)
                return Results.BadRequest("File size exceeds 50MB limit.");

            // Validate content type
            if (!storageService.ValidateFileType(file.ContentType, file.FileName))
                return Results.BadRequest($"Invalid file type. Allowed file types: .pdf, .jpg, .jpeg, .png, .gif, .doc, .docx, .xls, .xlsx, .txt");

            try
            {
                // Upload file to storage
                var storagePath = await storageService.UploadFileAsync(file);

                // Create attachment record
                var attachment = new Attachment
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    StoragePath = storagePath,
                    FileSize = file.Length,
                    Description = description,
                    Tags = tags != null ? JsonSerializer.Deserialize<string[]>(tags) : null,
                    EntityType = "Payment",
                    RentalPaymentId = paymentId
                };

                db.Attachments.Add(attachment);
                await db.SaveChangesAsync();

                return Results.Created($"/api/attachments/{attachment.Id}", attachment);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"File upload failed: {ex.Message}");
            }
        }).DisableAntiforgery();

    }
}

// Enum to specify attachment type
public enum RentalAttachmentType
{
    Property,
    Payment
}