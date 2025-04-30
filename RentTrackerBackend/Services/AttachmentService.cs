using Microsoft.Extensions.Logging;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using RentTrackerBackend.Endpoints;

namespace RentTrackerBackend.Services
{
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

    public class AttachmentService : IAttachmentService
    {
        private readonly IAttachmentRepository _attachmentRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<AttachmentService> _logger;
        private readonly IStorageService _storageService;
        private readonly IClaimsPrincipalService _claimsPrincipalService;

        public AttachmentService(
            IAttachmentRepository attachmentRepository,
            IPropertyRepository propertyRepository,
            IPaymentRepository paymentRepository,
            IStorageService storageService,
            IClaimsPrincipalService claimsPrincipalService,
            ILogger<AttachmentService> logger)
        {
            _attachmentRepository = attachmentRepository;
            _propertyRepository = propertyRepository;
            _paymentRepository = paymentRepository;
            _storageService = storageService;
            _claimsPrincipalService = claimsPrincipalService;
            _logger = logger;
        }

        public async Task<Attachment> SaveAttachmentAsync(
            IFormFile file,
            RentalAttachmentType attachmentType,
            string parentId,
            string? description = null,
            string[]? tags = null)
        {
            var tenantId = _claimsPrincipalService.GetTenantId();

            // Verify parent entity exists and get TenantId
            switch (attachmentType)
            {
                case RentalAttachmentType.Property:
                    var property = await _propertyRepository.GetByIdAsync(tenantId, parentId);
                    if (property == null)
                        throw new ArgumentException($"Property with ID {parentId} not found.");
                    tenantId = property.TenantId;
                    break;
                case RentalAttachmentType.Payment:
                    var payment = await _paymentRepository.GetByIdAsync(tenantId, parentId);
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

            await _attachmentRepository.CreateAsync(attachment);

            _logger.LogInformation($"Attachment saved: {attachment.FileName}");
            return attachment;
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadAttachmentAsync(string attachmentId)
        {
            var tenantId = _claimsPrincipalService.GetTenantId();
            var attachment = await _attachmentRepository.GetByIdAsync(tenantId, attachmentId);
            if (attachment == null)
                throw new FileNotFoundException($"Attachment with ID {attachmentId} not found.");

            var stream = await _storageService.DownloadFileAsync(attachment.StoragePath);
            return (stream, attachment.ContentType, attachment.FileName);
        }

        public async Task DeleteAttachmentAsync(string attachmentId)
        {
            var tenantId = _claimsPrincipalService.GetTenantId();
            var attachment = await _attachmentRepository.GetByIdAsync(tenantId, attachmentId);
            if (attachment == null)
                throw new FileNotFoundException($"Attachment with ID {attachmentId} not found.");

            // Delete the file from storage
            await _storageService.DeleteFileAsync(attachment.StoragePath);

            // Remove the database record
            await _attachmentRepository.DeleteAsync(attachment.TenantId, attachmentId);

            _logger.LogInformation($"Attachment deleted: {attachmentId}");
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentsForEntityAsync(RentalAttachmentType entityType, string entityId)
        {
            var tenantId = _claimsPrincipalService.GetTenantId();
            return entityType == RentalAttachmentType.Property
                ? await _attachmentRepository.GetAttachmentsByPropertyIdAsync(tenantId, entityId)
                : await _attachmentRepository.GetAttachmentsByPaymentIdAsync(tenantId, entityId);
        }
    }
}