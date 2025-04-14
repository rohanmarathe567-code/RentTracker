using RentTrackerBackend.Data;
using RentTrackerBackend.Endpoints;
using RentTrackerBackend.Models;
using MongoDB.Driver;

namespace RentTrackerBackend.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IMongoCollection<Attachment> _attachments;
    private readonly IMongoCollection<RentalProperty> _properties;
    private readonly IMongoCollection<RentalPayment> _payments;
    private readonly ILogger<AttachmentService> _logger;
    private readonly IStorageService _storageService;

    public AttachmentService(
        IMongoClient mongoClient,
        IStorageService storageService,
        ILogger<AttachmentService> logger)
    {
        var database = mongoClient.GetDatabase("renttracker");
        _attachments = database.GetCollection<Attachment>(nameof(Attachment));
        _properties = database.GetCollection<RentalProperty>(nameof(RentalProperty));
        _payments = database.GetCollection<RentalPayment>(nameof(RentalPayment));
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<Attachment> SaveAttachmentAsync(
        IFormFile file,
        RentalAttachmentType attachmentType,
        string parentId,
        string? description = null,
        string[]? tags = null)
    {
        // Verify parent entity exists and get TenantId
        string tenantId;
        switch (attachmentType)
        {
            case RentalAttachmentType.Property:
                var property = await _properties.Find(p => p.Id.ToString() == parentId).FirstOrDefaultAsync();
                if (property == null)
                    throw new ArgumentException($"Property with ID {parentId} not found.");
                tenantId = property.TenantId;
                break;
            case RentalAttachmentType.Payment:
                var payment = await _payments.Find(p => p.Id.ToString() == parentId).FirstOrDefaultAsync();
                if (payment == null)
                    throw new ArgumentException($"Payment with ID {parentId} not found.");
                tenantId = payment.TenantId;
                break;
            default:
                throw new ArgumentException($"Invalid attachment type: {attachmentType}");
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
            RentalPaymentId = attachmentType == RentalAttachmentType.Payment ? parentId : null,
            TenantId = tenantId
        };

        await _attachments.InsertOneAsync(attachment);

        _logger.LogInformation($"Attachment saved: {attachment.FileName}");
        return attachment;
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadAttachmentAsync(string attachmentId)
    {
        var attachment = await _attachments.Find(a => a.Id.ToString() == attachmentId).FirstOrDefaultAsync();
        if (attachment == null)
            throw new FileNotFoundException($"Attachment with ID {attachmentId} not found.");

        var stream = await _storageService.DownloadFileAsync(attachment.StoragePath);
        return (stream, attachment.ContentType, attachment.FileName);
    }

    public async Task DeleteAttachmentAsync(string attachmentId)
    {
        var attachment = await _attachments.Find(a => a.Id.ToString() == attachmentId).FirstOrDefaultAsync();
        if (attachment == null)
            throw new FileNotFoundException($"Attachment with ID {attachmentId} not found.");

        // Delete the file from storage
        await _storageService.DeleteFileAsync(attachment.StoragePath);

        // Remove the database record
        await _attachments.DeleteOneAsync(a => a.Id.ToString() == attachmentId);

        _logger.LogInformation($"Attachment deleted: {attachmentId}");
    }

    public async Task<IEnumerable<Attachment>> GetAttachmentsForEntityAsync(RentalAttachmentType entityType, string entityId)
    {
        var filter = entityType == RentalAttachmentType.Property
            ? Builders<Attachment>.Filter.Eq(a => a.RentalPropertyId, entityId)
            : Builders<Attachment>.Filter.Eq(a => a.RentalPaymentId, entityId);

        return await _attachments.Find(filter).ToListAsync();
    }
}