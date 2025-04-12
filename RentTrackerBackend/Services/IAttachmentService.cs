using RentTrackerBackend.Endpoints;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Services;

public interface IAttachmentService
{
    Task<Attachment> SaveAttachmentAsync(
        IFormFile file,
        RentalAttachmentType attachmentType,
        string parentId,
        string? description = null,
        string[]? tags = null);
        
    Task<(Stream FileStream, string ContentType, string FileName)> DownloadAttachmentAsync(string attachmentId);
    
    Task DeleteAttachmentAsync(string attachmentId);
    
    Task<IEnumerable<Attachment>> GetAttachmentsForEntityAsync(RentalAttachmentType entityType, string entityId);
}