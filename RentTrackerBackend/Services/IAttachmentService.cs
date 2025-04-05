using RentTrackerBackend.Endpoints;
using RentTrackerBackend.Models;
using System;

namespace RentTrackerBackend.Services;

public interface IAttachmentService
{
    Task<Attachment> SaveAttachmentAsync(
        IFormFile file,
        RentalAttachmentType attachmentType,
        Guid parentId,
        string? description = null,
        string[]? tags = null);
        
    Task<(Stream FileStream, string ContentType, string FileName)> DownloadAttachmentAsync(Guid attachmentId);
    
    Task DeleteAttachmentAsync(Guid attachmentId);
    
    Task<IEnumerable<Attachment>> GetAttachmentsForEntityAsync(RentalAttachmentType entityType, Guid entityId);
}