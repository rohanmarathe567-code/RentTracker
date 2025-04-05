using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using System;

namespace RentTrackerBackend.Services;

public class FileService : IStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _uploadsFolder;
    private readonly ILogger<FileService> _logger;

    public FileService(IWebHostEnvironment environment, ILogger<FileService> logger)
    {
        _environment = environment;
        _logger = logger;
        _uploadsFolder = Path.Combine(AppContext.BaseDirectory, "uploads");
        
        try
        {
            // Ensure uploads directory exists
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
                _logger.LogInformation($"Created uploads directory at: {_uploadsFolder}");
            }

            // Verify directory is writable
            var testFile = Path.Combine(_uploadsFolder, "test.txt");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            _logger.LogInformation("Verified uploads directory is writable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize uploads directory at: {_uploadsFolder}");
            throw new InvalidOperationException($"Failed to initialize uploads directory: {ex.Message}", ex);
        }
    }

    private readonly Dictionary<string, string[]> _allowedFileTypes = new()
    {
        { ".pdf", new[] { "application/pdf" } },
        { ".jpg", new[] { "image/jpeg", "image/jpg", "application/octet-stream" } },
        { ".jpeg", new[] { "image/jpeg", "image/jpg", "application/octet-stream" } },
        { ".png", new[] { "image/png", "application/octet-stream" } },
        { ".gif", new[] { "image/gif", "application/octet-stream" } },
        { ".doc", new[] { "application/msword", "application/octet-stream" } },
        { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/octet-stream" } },
        { ".xls", new[] { "application/vnd.ms-excel", "application/octet-stream" } },
        { ".xlsx", new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/octet-stream" } },
        { ".txt", new[] { "text/plain", "application/octet-stream" } }
    };

    public bool ValidateFileType(string contentType, string fileName)
    {
        _logger.LogDebug($"Validating file type: {contentType} for file: {fileName}");
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_allowedFileTypes.ContainsKey(extension))
        {
            _logger.LogWarning($"File extension not allowed: {extension}");
            return false;
        }

        var normalizedContentType = contentType.ToLowerInvariant();
        var isValid = _allowedFileTypes[extension].Contains(normalizedContentType);
        
        _logger.LogDebug($"File type validation result: {isValid} for content type: {normalizedContentType}");
        return isValid;
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        try
        {
            _logger.LogInformation($"Attempting to upload file: {file.FileName} ({file.ContentType})");
            
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("File is null or empty");
                throw new ArgumentException("File is null or empty");
            }

            if (!ValidateFileType(file.ContentType, file.FileName))
            {
                var extension = Path.GetExtension(file.FileName);
                _logger.LogWarning($"Invalid file type: {file.ContentType} for file: {file.FileName}");
                throw new InvalidOperationException(
                    $"File type not allowed. Content-Type: {file.ContentType}, Extension: {extension}. " +
                    "Allowed file types: .pdf, .jpg, .jpeg, .png, .gif, .doc, .docx, .xls, .xlsx, .txt");
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(_uploadsFolder, uniqueFileName);
            
            _logger.LogDebug($"Saving file to: {filePath}");
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation($"Successfully uploaded file: {uniqueFileName}");
            return uniqueFileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to upload file: {file.FileName}");
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string storagePath)
    {
        try
        {
            var filePath = Path.Combine(_uploadsFolder, storagePath);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"File not found: {filePath}");
                throw new FileNotFoundException($"File not found: {storagePath}");
            }

            _logger.LogDebug($"Opening file for download: {filePath}");
            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading file: {storagePath}");
            throw;
        }
    }

    public async Task DeleteFileAsync(string storagePath)
    {
        try
        {
            var filePath = Path.Combine(_uploadsFolder, storagePath);
            
            // Use Task.Run for synchronous file operations
            await Task.Run(() =>
            {
                if (File.Exists(filePath))
                {
                    _logger.LogInformation($"Deleting file: {filePath}");
                    File.Delete(filePath);
                    _logger.LogInformation($"Successfully deleted file: {storagePath}");
                }
                else
                {
                    _logger.LogWarning($"File not found for deletion: {filePath}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting file: {storagePath}");
            throw;
        }
    }
}