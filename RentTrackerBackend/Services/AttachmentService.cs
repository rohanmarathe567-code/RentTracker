using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Endpoints;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Services;

public class AttachmentService : IAttachmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AttachmentService> _logger;
    private readonly IStorageService _storageService;

    public AttachmentService(
        ApplicationDbContext context,
        IStorageService storageService,
        ILogger<AttachmentService> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<Attachment> SaveAttachmentAsync(
        IFormFile file,
        RentalAttachmentType attachmentType,
        Guid parentId,
        string? description = null,
        string[]? tags = null)
    {
        // Verify parent entity exists
        switch (attachmentType)
        {
            case RentalAttachmentType.Property:
                var property = await _context.RentalProperties.FindAsync(parentId);
                if (property == null)
                    throw new ArgumentException($"Property with ID {parentId} not found.");
                break;
            case RentalAttachmentType.Payment:
                var payment = await _context.RentalPayments.FindAsync(parentId);
                if (payment == null)
                    throw new ArgumentException($"Payment with ID {parentId} not found.");
                break;
        }

        // Validate and store the file
        if (!_storageService.ValidateFileType(file.ContentType, file.FileName))
        {
            var extension = Path.GetExtension(file.FileName);
            throw new InvalidOperationException(
                $"File type not allowed. Content-Type: {file.ContentType}, Extension: {extension}. " +
                "Allowed file types: .pdf, .jpg, .jpeg, .png, .gif, .doc, .docx, .xls, .xlsx, .txt");
        }

        // Upload file to storage
        var storagePath = await _storageService.UploadFileAsync(file);

        // Create attachment record
        var attachment = new Attachment
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            StoragePath = storagePath,
            FileSize = file.Length,
            Description = description,
            Tags = tags,
            EntityType = attachmentType.ToString(),
            RentalPropertyId = attachmentType == RentalAttachmentType.Property ? parentId : null,
            RentalPaymentId = attachmentType == RentalAttachmentType.Payment ? parentId : null
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Attachment saved: {attachment.FileName}");
        return attachment;
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _context.Attachments.FindAsync(attachmentId);
        if (attachment == null)
            throw new FileNotFoundException($"Attachment with ID {attachmentId} not found.");

        var stream = await _storageService.DownloadFileAsync(attachment.StoragePath);
        return (stream, attachment.ContentType, attachment.FileName);
    }

    public async Task DeleteAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _context.Attachments.FindAsync(attachmentId);
        if (attachment == null)
            throw new FileNotFoundException($"Attachment with ID {attachmentId} not found.");

        // Delete the file from storage
        await _storageService.DeleteFileAsync(attachment.StoragePath);

        // Remove the database record
        _context.Attachments.Remove(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Attachment deleted: {attachmentId}");
    }

    public async Task<IEnumerable<Attachment>> GetAttachmentsForEntityAsync(RentalAttachmentType entityType, Guid entityId)
    {
        return await _context.Attachments
            .Where(a =>
                (entityType == RentalAttachmentType.Property && a.RentalPropertyId == entityId) ||
                (entityType == RentalAttachmentType.Payment && a.RentalPaymentId == entityId))
            .ToListAsync();
    }
}