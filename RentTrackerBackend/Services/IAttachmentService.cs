using RentTrackerBackend.Endpoints;
using RentTrackerBackend.Models;
using System;

namespace RentTrackerBackend.Services;

public interface IAttachmentService
{
    Task<Attachment> SaveAttachmentAsync(
        IFormFile file,
        RentalAttachmentType attachmentType,
        Guid parentId);
}