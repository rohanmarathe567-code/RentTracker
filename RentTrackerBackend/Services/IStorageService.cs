using Microsoft.AspNetCore.Http;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Services;

public interface IStorageService
{
    Task<string> UploadFileAsync(IFormFile file);
    Task<Stream> DownloadFileAsync(string storagePath);
    Task DeleteFileAsync(string storagePath);
    bool ValidateFileType(string contentType, string fileName);
}