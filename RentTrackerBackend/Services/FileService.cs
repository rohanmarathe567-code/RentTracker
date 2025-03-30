using RentTrackerBackend.Data;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Services;

public class FileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _dbContext;
    private readonly string _uploadsFolder;

    public FileService(IWebHostEnvironment environment, ApplicationDbContext dbContext)
    {
        _environment = environment;
        _dbContext = dbContext;
        _uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads");
        
        // Ensure uploads directory exists
        if (!Directory.Exists(_uploadsFolder))
        {
            Directory.CreateDirectory(_uploadsFolder);
        }
    }

    public async Task<Attachment> SaveFileAsync(IFormFile file, string? description = null, int? propertyId = null, int? paymentId = null)
    {
        // Generate a unique filename to prevent collisions
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(_uploadsFolder, uniqueFileName);

        // Save the file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create attachment record in database
        var attachment = new Attachment
        {
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            FilePath = uniqueFileName,
            FileSize = file.Length,
            Description = description,
            UploadDate = DateTime.UtcNow,
            RentalPropertyId = propertyId,
            RentalPaymentId = paymentId
        };

        _dbContext.Attachments.Add(attachment);
        await _dbContext.SaveChangesAsync();

        return attachment;
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> GetFileAsync(int attachmentId)
    {
        var attachment = await _dbContext.Attachments.FindAsync(attachmentId);
        
        if (attachment == null)
        {
            throw new FileNotFoundException("Attachment not found");
        }

        var filePath = Path.Combine(_uploadsFolder, attachment.FilePath!);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found on disk");
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return (stream, attachment.ContentType ?? "application/octet-stream", attachment.FileName);
    }

    public async Task DeleteFileAsync(int attachmentId)
    {
        var attachment = await _dbContext.Attachments.FindAsync(attachmentId);
        
        if (attachment == null)
        {
            throw new FileNotFoundException("Attachment not found");
        }

        var filePath = Path.Combine(_uploadsFolder, attachment.FilePath!);
        
        // Remove from database
        _dbContext.Attachments.Remove(attachment);
        await _dbContext.SaveChangesAsync();
        
        // Delete physical file if it exists
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}