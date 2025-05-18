using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        })
        .RequireAuthorization();

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
        })
        .RequireAuthorization();

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
        })
        .RequireAuthorization();

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
        })
        .DisableAntiforgery()
        .RequireAuthorization();

        // Transaction Attachments
        app.MapGet("/api/properties/{propertyId}/transactions/{transactionId}/attachments", async (
            string propertyId,
            string transactionId,
            IAttachmentService attachmentService,
            IMongoRepository<RentalProperty> propertyRepository,
            IPropertyTransactionRepository transactionRepository,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            var property = await propertyRepository.GetByIdAsync(tenantId, propertyId);
            if (property == null)
                return Results.NotFound("Property not found");

            var transaction = await transactionRepository.GetByIdAsync(tenantId, transactionId);
            if (transaction == null || transaction.RentalPropertyId != propertyId)
                return Results.NotFound("Transaction not found");

            var attachments = await attachmentService.GetAttachmentsForEntityAsync(RentalAttachmentType.Transaction, transactionId);
            return Results.Ok(attachments);
        })
        .RequireAuthorization();

        app.MapPost("/api/properties/{propertyId}/transactions/{transactionId}/attachments", async (
            string propertyId,
            string transactionId,
            IFormFile file,
            IAttachmentService attachmentService,
            IMongoRepository<RentalProperty> propertyRepository,
            IPropertyTransactionRepository transactionRepository,
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

            var transaction = await transactionRepository.GetByIdAsync(tenantId, transactionId);
            if (transaction == null || transaction.RentalPropertyId != propertyId)
                return Results.NotFound("Transaction not found");

            try
            {
                var parsedTags = tags != null ? JsonSerializer.Deserialize<string[]>(tags) : null;
                var attachment = await attachmentService.SaveAttachmentAsync(file, RentalAttachmentType.Transaction, transactionId, description, parsedTags);
                return Results.Created($"/api/attachments/{attachment.Id}", attachment);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading attachment for transaction {Id}", transactionId);
                return Results.BadRequest($"File upload failed: {ex.Message}");
            }
        })
        .DisableAntiforgery()
        .RequireAuthorization();
    }
}