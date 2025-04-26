using Microsoft.AspNetCore.Mvc;
using RentTrackerBackend.Models;
using RentTrackerBackend.Services;
using RentTrackerBackend.Data;
using System.Text.Json;
using MongoDB.Driver;

namespace RentTrackerBackend.Endpoints;

public static class AttachmentsController
{
    public static void MapAttachmentEndpoints(this WebApplication app)
    {
        // Download attachment
        app.MapGet("/api/attachments/{attachmentId}/download", async (
            string attachmentId,
            IAttachmentService attachmentService,
            IClaimsPrincipalService claimsPrincipalService,
            ILogger<Program> logger) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var (stream, contentType, fileName) = await attachmentService.DownloadAttachmentAsync(attachmentId);
                return Results.File(stream, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error downloading attachment {Id}", attachmentId);
                return Results.BadRequest($"File download failed: {ex.Message}");
            }
        });

        // Delete attachment
        app.MapDelete("/api/attachments/{attachmentId}", async (
            string attachmentId,
            IAttachmentService attachmentService,
            IClaimsPrincipalService claimsPrincipalService,
            ILogger<Program> logger) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            try
            {
                await attachmentService.DeleteAttachmentAsync(attachmentId);
                return Results.NoContent();
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting attachment {Id}", attachmentId);
                return Results.BadRequest($"File deletion failed: {ex.Message}");
            }
        });

        // Property Attachments
        app.MapGet("/api/properties/{propertyId}/attachments", async (
            string propertyId,
            IAttachmentService attachmentService,
            IMongoRepository<RentalProperty> propertyRepository,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            var property = await propertyRepository.GetByIdAsync(tenantId, propertyId);
            if (property == null)
                return Results.NotFound("Property not found");

            var attachments = await attachmentService.GetAttachmentsForEntityAsync(RentalAttachmentType.Property, propertyId);
            return Results.Ok(attachments);
        });

        app.MapPost("/api/properties/{propertyId}/attachments", async (
            string propertyId,
            IFormFile file,
            IAttachmentService attachmentService,
            IMongoRepository<RentalProperty> propertyRepository,
            IClaimsPrincipalService claimsPrincipalService,
            ILogger<Program> logger,
            string? description = null,
            string? tags = null) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            var property = await propertyRepository.GetByIdAsync(tenantId, propertyId);
            if (property == null)
                return Results.NotFound("Property not found");

            try
            {
                var parsedTags = tags != null ? JsonSerializer.Deserialize<string[]>(tags) : null;
                var attachment = await attachmentService.SaveAttachmentAsync(file, RentalAttachmentType.Property, propertyId, description, parsedTags);
                return Results.Created($"/api/attachments/{attachment.Id}", attachment);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading attachment for property {Id}", propertyId);
                return Results.BadRequest($"File upload failed: {ex.Message}");
            }
        }).DisableAntiforgery();

        // Payment Attachments
        app.MapGet("/api/properties/{propertyId}/payments/{paymentId}/attachments", async (
            string propertyId,
            string paymentId,
            IAttachmentService attachmentService,
            IMongoRepository<RentalProperty> propertyRepository,
            IMongoRepository<RentalPayment> paymentRepository,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            var property = await propertyRepository.GetByIdAsync(tenantId, propertyId);
            if (property == null)
                return Results.NotFound("Property not found");

            var payment = await paymentRepository.GetByIdAsync(tenantId, paymentId);
            if (payment == null || payment.RentalPropertyId != propertyId)
                return Results.NotFound("Payment not found");

            var attachments = await attachmentService.GetAttachmentsForEntityAsync(RentalAttachmentType.Payment, paymentId);
            return Results.Ok(attachments);
        });

        app.MapPost("/api/properties/{propertyId}/payments/{paymentId}/attachments", async (
            string propertyId,
            string paymentId,
            IFormFile file,
            IAttachmentService attachmentService,
            IMongoRepository<RentalProperty> propertyRepository,
            IMongoRepository<RentalPayment> paymentRepository,
            IClaimsPrincipalService claimsPrincipalService,
            ILogger<Program> logger,
            string? description = null,
            string? tags = null) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            var property = await propertyRepository.GetByIdAsync(tenantId, propertyId);
            if (property == null)
                return Results.NotFound("Property not found");

            var payment = await paymentRepository.GetByIdAsync(tenantId, paymentId);
            if (payment == null || payment.RentalPropertyId != propertyId)
                return Results.NotFound("Payment not found");

            try
            {
                var parsedTags = tags != null ? JsonSerializer.Deserialize<string[]>(tags) : null;
                var attachment = await attachmentService.SaveAttachmentAsync(file, RentalAttachmentType.Payment, paymentId, description, parsedTags);
                return Results.Created($"/api/attachments/{attachment.Id}", attachment);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading attachment for payment {Id}", paymentId);
                return Results.BadRequest($"File upload failed: {ex.Message}");
            }
        }).DisableAntiforgery();
    }
}