using Microsoft.EntityFrameworkCore;
using RentTracker.Data;
using RentTracker.Services;

namespace RentTracker.Api.Endpoints;

public static class AttachmentsController
{
    public static void MapAttachmentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/attachments/{id}", async (int id, ApplicationDbContext db) =>
            await db.Attachments.FindAsync(id) is { } attachment
                ? Results.Ok(attachment)
                : Results.NotFound());

        app.MapGet("/api/properties/{propertyId}/attachments", async (int propertyId, ApplicationDbContext db) =>
            await db.Attachments
                .Where(a => a.RentalPropertyId == propertyId)
                .OrderByDescending(a => a.UploadDate)
                .ToListAsync());

        app.MapGet("/api/payments/{paymentId}/attachments", async (int paymentId, ApplicationDbContext db) =>
            await db.Attachments
                .Where(a => a.RentalPaymentId == paymentId)
                .OrderByDescending(a => a.UploadDate)
                .ToListAsync());

        app.MapPost("/api/properties/{propertyId}/attachments", async (int propertyId, HttpRequest request, FileService fileService, ApplicationDbContext db) =>
        {
            var property = await db.RentalProperties.FindAsync(propertyId);
            if (property == null)
                return Results.NotFound("Property not found");

            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.BadRequest("No files were uploaded");

            var file = request.Form.Files[0];
            var description = request.Form["description"].ToString();

            var attachment = await fileService.SaveFileAsync(file, description, propertyId, null);
            return Results.Created($"/api/attachments/{attachment.Id}", attachment);
        });

        app.MapPost("/api/payments/{paymentId}/attachments", async (int paymentId, HttpRequest request, FileService fileService, ApplicationDbContext db) =>
        {
            var payment = await db.RentalPayments.FindAsync(paymentId);
            if (payment == null)
                return Results.NotFound("Payment not found");

            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.BadRequest("No files were uploaded");

            var file = request.Form.Files[0];
            var description = request.Form["description"].ToString();

            var attachment = await fileService.SaveFileAsync(file, description, null, paymentId);
            return Results.Created($"/api/attachments/{attachment.Id}", attachment);
        });

        app.MapGet("/api/attachments/{id}/download", async (int id, FileService fileService) =>
        {
            try
            {
                var (fileStream, contentType, fileName) = await fileService.GetFileAsync(id);
                return Results.File(fileStream, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound("File not found");
            }
        });

        app.MapDelete("/api/attachments/{id}", async (int id, FileService fileService) =>
        {
            try
            {
                await fileService.DeleteFileAsync(id);
                return Results.NoContent();
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound("Attachment not found");
            }
        });
    }
}