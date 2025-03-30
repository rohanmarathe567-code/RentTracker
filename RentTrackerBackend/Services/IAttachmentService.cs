using RentTrackerBackend.Endpoints;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Services;

public interface IAttachmentService
{
    Task<Attachment> SaveAttachmentAsync(
        IFormFile file, 
        RentalAttachmentType attachmentType, 
        int parentId);
}