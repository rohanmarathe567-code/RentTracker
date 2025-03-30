using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Endpoints;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Services;

public class AttachmentService : IAttachmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AttachmentService> _logger;

    // Allowed file types and max file size
    private static readonly string[] AllowedFileTypes = new[] 
    { 
        ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".gif", 
        ".bmp", ".tiff", ".txt", ".csv", ".xls", ".xlsx" 
    };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public AttachmentService(
        ApplicationDbContext context, 
        IWebHostEnvironment environment,
        ILogger<AttachmentService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<Attachment> SaveAttachmentAsync(
        IFormFile file,
        RentalAttachmentType attachmentType,
        Guid parentId)
    {
        // Validate file
        ValidateFile(file);

        // Determine upload directory
        var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", attachmentType.ToString().ToLower());
        Directory.CreateDirectory(uploadDir);

        // Generate unique filename
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadDir, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create attachment record
        var attachment = new Attachment
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            FilePath = Path.Combine("uploads", attachmentType.ToString().ToLower(), fileName),
            FileSize = file.Length,
            UploadDate = DateTime.UtcNow
        };

        // Set parent based on attachment type
        switch (attachmentType)
        {
            case RentalAttachmentType.Property:
                var property = await _context.RentalProperties.FindAsync(parentId);
                if (property == null)
                    throw new ArgumentException($"Property with ID {parentId} not found.");
                attachment.RentalPropertyId = parentId;
                break;
            case RentalAttachmentType.Payment:
                var payment = await _context.RentalPayments.FindAsync(parentId);
                if (payment == null)
                    throw new ArgumentException($"Payment with ID {parentId} not found.");
                attachment.RentalPaymentId = parentId;
                break;
        }

        // Save to database
        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Attachment saved: {attachment.FileName}");
        return attachment;
    }

    private void ValidateFile(IFormFile file)
    {
        // Check file size
        if (file.Length > MaxFileSize)
            throw new ArgumentException($"File size exceeds maximum limit of {MaxFileSize / 1024 / 1024}MB");

        // Check file extension
        var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedFileTypes.Contains(fileExt))
            throw new ArgumentException($"File type {fileExt} is not allowed. Allowed types: {string.Join(", ", AllowedFileTypes)}");
    }
}