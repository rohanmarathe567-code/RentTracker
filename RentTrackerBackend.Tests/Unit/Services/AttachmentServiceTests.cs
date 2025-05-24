// filepath: c:\git\RentTracker\RentTrackerBackend.Tests\Unit\Services\AttachmentServiceTests.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Services;
using System.Text;

namespace RentTrackerBackend.Tests.Unit.Services;

public class AttachmentServiceTests
{
    private readonly IAttachmentRepository _mockAttachmentRepository;
    private readonly IPropertyRepository _mockPropertyRepository;
    private readonly IPropertyTransactionRepository _mockTransactionRepository;
    private readonly IStorageService _mockStorageService;
    private readonly IClaimsPrincipalService _mockClaimsPrincipalService;
    private readonly ILogger<AttachmentService> _mockLogger;
    private readonly AttachmentService _attachmentService;
    
    private const string TestTenantId = "507f1f77bcf86cd799439011";
    private const string TestPropertyId = "507f1f77bcf86cd799439012";
    private const string TestAttachmentId = "507f1f77bcf86cd799439014";
    private const string TestStoragePath = "uploads/2025/04/30/test.pdf";
    
    public AttachmentServiceTests()
    {
        _mockAttachmentRepository = Substitute.For<IAttachmentRepository>();
        _mockPropertyRepository = Substitute.For<IPropertyRepository>();
        _mockTransactionRepository = Substitute.For<IPropertyTransactionRepository>();
        _mockStorageService = Substitute.For<IStorageService>();
        _mockClaimsPrincipalService = Substitute.For<IClaimsPrincipalService>();
        _mockLogger = Substitute.For<ILogger<AttachmentService>>();
        
        _mockClaimsPrincipalService.GetTenantId().Returns(TestTenantId);
        
        _attachmentService = new AttachmentService(
            _mockAttachmentRepository,
            _mockPropertyRepository,
            _mockTransactionRepository,
            _mockStorageService,
            _mockClaimsPrincipalService,
            _mockLogger);
    }
    
    #region SaveAttachmentAsync Tests
    
    [Fact]
    public async Task SaveAttachmentAsync_WithPropertyAttachment_ValidFile_CreatesAttachment()
    {
        // Arrange
        var mockFile = CreateMockFormFile("test.pdf", "application/pdf", 1024);
        var property = new RentalProperty { Id = ObjectId.Parse(TestPropertyId), TenantId = TestTenantId };
        var description = "Test description";
        var tags = new[] { "test", "document" };
        
        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(property);
        _mockStorageService.ValidateFileType(mockFile.ContentType, mockFile.FileName).Returns(true);
        _mockStorageService.UploadFileAsync(mockFile).Returns(TestStoragePath);
        
        // Act
        var result = await _attachmentService.SaveAttachmentAsync(
            mockFile, 
            RentalAttachmentType.Property, 
            TestPropertyId, 
            description, 
            tags);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockFile.FileName, result.FileName);
        Assert.Equal(mockFile.ContentType, result.ContentType);
        Assert.Equal(TestStoragePath, result.StoragePath);
        Assert.Equal(mockFile.Length, result.FileSize);
        Assert.Equal(description, result.Description);
        Assert.Equal(tags, result.Tags);
        Assert.Equal(RentalAttachmentType.Property.ToString(), result.EntityType);
        Assert.Equal(TestPropertyId, result.RentalPropertyId);
        Assert.Equal(TestTenantId, result.TenantId);
        
        await _mockPropertyRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockStorageService.Received(1).UploadFileAsync(mockFile);
        await _mockAttachmentRepository.Received(1).CreateAsync(Arg.Any<Attachment>());
    }
    
    
    [Fact]
    public async Task SaveAttachmentAsync_WithInvalidFileType_ThrowsException()
    {
        // Arrange
        var mockFile = CreateMockFormFile("malicious.exe", "application/octet-stream", 1024);
        var property = new RentalProperty { Id = ObjectId.Parse(TestPropertyId), TenantId = TestTenantId };
        
        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(property);
        _mockStorageService.ValidateFileType(mockFile.ContentType, mockFile.FileName).Returns(false);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _attachmentService.SaveAttachmentAsync(mockFile, RentalAttachmentType.Property, TestPropertyId));
        
        Assert.Contains("File type not allowed", exception.Message);
        
        await _mockPropertyRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockStorageService.DidNotReceive().UploadFileAsync(Arg.Any<IFormFile>());
        await _mockAttachmentRepository.DidNotReceive().CreateAsync(Arg.Any<Attachment>());
    }
    
    [Fact]
    public async Task SaveAttachmentAsync_WithNonExistentProperty_ThrowsException()
    {
        // Arrange
        var mockFile = CreateMockFormFile("test.pdf", "application/pdf", 1024);
        
        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns((RentalProperty)null!);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _attachmentService.SaveAttachmentAsync(mockFile, RentalAttachmentType.Property, TestPropertyId));
        
        Assert.Contains($"Property with ID {TestPropertyId} not found", exception.Message);
        
        _ = _mockStorageService.DidNotReceive().ValidateFileType(Arg.Any<string>(), Arg.Any<string>());
        _ = _mockStorageService.DidNotReceive().UploadFileAsync(Arg.Any<IFormFile>());
        _ = _mockAttachmentRepository.DidNotReceive().CreateAsync(Arg.Any<Attachment>());
    }
    
    
    [Fact]
    public async Task SaveAttachmentAsync_WithInvalidAttachmentType_ThrowsException()
    {
        // Arrange
        var mockFile = CreateMockFormFile("test.pdf", "application/pdf", 1024);
        var invalidType = (RentalAttachmentType)99; // Invalid enum value
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _attachmentService.SaveAttachmentAsync(mockFile, invalidType, TestPropertyId));
        
        Assert.Contains($"Invalid attachment type: {invalidType}", exception.Message);
        
        _ = _mockPropertyRepository.DidNotReceive().GetByIdAsync(Arg.Any<string>(), Arg.Any<string>());
        _ = _mockStorageService.DidNotReceive().ValidateFileType(Arg.Any<string>(), Arg.Any<string>());
        _ = _mockStorageService.DidNotReceive().UploadFileAsync(Arg.Any<IFormFile>());
        _ = _mockAttachmentRepository.DidNotReceive().CreateAsync(Arg.Any<Attachment>());
    }
    
    #endregion
    
    #region DownloadAttachmentAsync Tests
    
    [Fact]
    public async Task DownloadAttachmentAsync_WithExistingAttachment_ReturnsFileStream()
    {
        // Arrange
        var attachment = new Attachment
        {
            Id = ObjectId.Parse(TestAttachmentId),
            FileName = "test.pdf",
            ContentType = "application/pdf",
            StoragePath = TestStoragePath,
            TenantId = TestTenantId
        };
        
        var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("Test file content"));
        
        _mockAttachmentRepository.GetByIdAsync(TestTenantId, TestAttachmentId).Returns(attachment);
        _mockStorageService.DownloadFileAsync(TestStoragePath).Returns(expectedStream);
        
        // Act
        var (fileStream, contentType, fileName) = await _attachmentService.DownloadAttachmentAsync(TestAttachmentId);
        
        // Assert
        Assert.NotNull(fileStream);
        Assert.Equal(expectedStream, fileStream);
        Assert.Equal(attachment.ContentType, contentType);
        Assert.Equal(attachment.FileName, fileName);
        
        await _mockAttachmentRepository.Received(1).GetByIdAsync(TestTenantId, TestAttachmentId);
        await _mockStorageService.Received(1).DownloadFileAsync(TestStoragePath);
    }
    
    [Fact]
    public async Task DownloadAttachmentAsync_WithNonExistentAttachment_ThrowsException()
    {
        // Arrange
        _mockAttachmentRepository.GetByIdAsync(TestTenantId, TestAttachmentId).Returns((Attachment)null!);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _attachmentService.DownloadAttachmentAsync(TestAttachmentId));
        
        Assert.Contains($"Attachment with ID {TestAttachmentId} not found", exception.Message);
        
        await _mockStorageService.DidNotReceive().DownloadFileAsync(Arg.Any<string>());
    }
    
    [Fact]
    public async Task DownloadAttachmentAsync_WhenStorageServiceFails_PropagatesException()
    {
        // Arrange
        var attachment = new Attachment
        {
            Id = ObjectId.Parse(TestAttachmentId),
            FileName = "test.pdf",
            ContentType = "application/pdf",
            StoragePath = TestStoragePath,
            TenantId = TestTenantId
        };
        
        var expectedException = new IOException("Storage error");
        
        _mockAttachmentRepository.GetByIdAsync(TestTenantId, TestAttachmentId).Returns(attachment);
        _mockStorageService.DownloadFileAsync(TestStoragePath).Throws(expectedException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<IOException>(() => 
            _attachmentService.DownloadAttachmentAsync(TestAttachmentId));
        
        Assert.Equal(expectedException, exception);
        
        await _mockAttachmentRepository.Received(1).GetByIdAsync(TestTenantId, TestAttachmentId);
        await _mockStorageService.Received(1).DownloadFileAsync(TestStoragePath);
    }
    
    #endregion
    
    #region DeleteAttachmentAsync Tests
    
    [Fact]
    public async Task DeleteAttachmentAsync_WithExistingAttachment_DeletesSuccessfully()
    {
        // Arrange
        var attachment = new Attachment
        {
            Id = ObjectId.Parse(TestAttachmentId),
            StoragePath = TestStoragePath,
            TenantId = TestTenantId
        };
        
        _mockAttachmentRepository.GetByIdAsync(TestTenantId, TestAttachmentId).Returns(attachment);
        
        // Act
        await _attachmentService.DeleteAttachmentAsync(TestAttachmentId);
        
        // Assert
        await _mockAttachmentRepository.Received(1).GetByIdAsync(TestTenantId, TestAttachmentId);
        await _mockStorageService.Received(1).DeleteFileAsync(TestStoragePath);
        await _mockAttachmentRepository.Received(1).DeleteAsync(TestTenantId, TestAttachmentId);
    }
    
    [Fact]
    public async Task DeleteAttachmentAsync_WithNonExistentAttachment_ThrowsException()
    {
        // Arrange
        _mockAttachmentRepository.GetByIdAsync(TestTenantId, TestAttachmentId).Returns((Attachment)null!);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _attachmentService.DeleteAttachmentAsync(TestAttachmentId));
        
        Assert.Contains($"Attachment with ID {TestAttachmentId} not found", exception.Message);
        
        _ = _mockStorageService.DidNotReceive().DeleteFileAsync(Arg.Any<string>());
        _ = _mockAttachmentRepository.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<string>());
    }
    
    [Fact]
    public async Task DeleteAttachmentAsync_WhenStorageServiceFailsButContinues_DeletesRecord()
    {
        // Arrange
        var attachment = new Attachment
        {
            Id = ObjectId.Parse(TestAttachmentId),
            StoragePath = TestStoragePath,
            TenantId = TestTenantId
        };
        
        _mockAttachmentRepository.GetByIdAsync(TestTenantId, TestAttachmentId).Returns(attachment);
        _mockStorageService.DeleteFileAsync(TestStoragePath).ThrowsForAnyArgs(new FileNotFoundException("File not found in storage"));
        
        // Act & Assert (no exception should be thrown)
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _attachmentService.DeleteAttachmentAsync(TestAttachmentId));
        
        await _mockAttachmentRepository.Received(1).GetByIdAsync(TestTenantId, TestAttachmentId);
        await _mockAttachmentRepository.DidNotReceive().DeleteAsync(TestTenantId, TestAttachmentId);
    }
    
    #endregion
    
    #region Transaction Attachment Tests

    [Fact]
    public async Task SaveAttachmentAsync_WithTransactionAttachment_ValidFile_CreatesAttachment()
    {
        // Arrange
        var mockFile = CreateMockFormFile("test.pdf", "application/pdf", 1024);
        var transactionId = TestPropertyId;  // Using TestPropertyId for consistent test data
        var transaction = new PropertyTransaction { Id = ObjectId.Parse(transactionId), TenantId = TestTenantId };
        var description = "Test transaction doc";
        var tags = new[] { "transaction", "document" };
        
        _mockTransactionRepository.GetByIdAsync(TestTenantId, transactionId).Returns(transaction);
        _mockStorageService.ValidateFileType(mockFile.ContentType, mockFile.FileName).Returns(true);
        _mockStorageService.UploadFileAsync(mockFile).Returns(TestStoragePath);
        
        // Act
        var result = await _attachmentService.SaveAttachmentAsync(
            mockFile,
            RentalAttachmentType.Transaction,
            transactionId,
            description,
            tags);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockFile.FileName, result.FileName);
        Assert.Equal(mockFile.ContentType, result.ContentType);
        Assert.Equal(TestStoragePath, result.StoragePath);
        Assert.Equal(mockFile.Length, result.FileSize);
        Assert.Equal(description, result.Description);
        Assert.Equal(tags, result.Tags);
        Assert.Equal(RentalAttachmentType.Transaction.ToString(), result.EntityType);
        Assert.Null(result.RentalPropertyId);
        Assert.Equal(TestPropertyId, result.TransactionId);
        Assert.Equal(TestTenantId, result.TenantId);
        
        await _mockTransactionRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockStorageService.Received(1).UploadFileAsync(mockFile);
        await _mockAttachmentRepository.Received(1).CreateAsync(Arg.Any<Attachment>());
    }

    [Fact]
    public async Task GetAttachmentsForEntityAsync_ForTransaction_ReturnsAttachments()
    {
        // Arrange
        var transactionId = "507f1f77bcf86cd799439013";
        var expectedAttachments = new List<Attachment>
        {
            new Attachment {
                Id = ObjectId.Parse("507f1f77bcf86cd799439015"),
                FileName = "invoice.pdf",
                RentalPropertyId = TestPropertyId,
                TransactionId = transactionId
            },
            new Attachment {
                Id = ObjectId.Parse("507f1f77bcf86cd799439016"),
                FileName = "receipt.pdf",
                RentalPropertyId = TestPropertyId,
                TransactionId = transactionId
            }
        };
        
        _mockAttachmentRepository.GetAttachmentsByTransactionIdAsync(TestTenantId, transactionId)
            .Returns(expectedAttachments);
        
        // Act
        var result = await _attachmentService.GetAttachmentsForEntityAsync(
            RentalAttachmentType.Transaction,
            transactionId);
        
        // Assert
        Assert.Equal(expectedAttachments.Count, result.Count());
        Assert.Equal(expectedAttachments, result);
        
        await _mockAttachmentRepository.Received(1)
            .GetAttachmentsByTransactionIdAsync(TestTenantId, transactionId);
    }

    #endregion

    #region GetAttachmentsForEntityAsync Tests
    
    [Fact]
    public async Task GetAttachmentsForEntityAsync_ForProperty_ReturnsAttachments()
    {
        // Arrange
        var expectedAttachments = new List<Attachment>
        {
            new Attachment { Id = ObjectId.Parse("507f1f77bcf86cd799439015"), FileName = "doc1.pdf", RentalPropertyId = TestPropertyId },
            new Attachment { Id = ObjectId.Parse("507f1f77bcf86cd799439016"), FileName = "doc2.pdf", RentalPropertyId = TestPropertyId }
        };
        
        _mockAttachmentRepository.GetAttachmentsByPropertyIdAsync(TestTenantId, TestPropertyId)
            .Returns(expectedAttachments);
        
        // Act
        var result = await _attachmentService.GetAttachmentsForEntityAsync(RentalAttachmentType.Property, TestPropertyId);
        
        // Assert
        Assert.Equal(expectedAttachments.Count, result.Count());
        Assert.Equal(expectedAttachments, result);
        
        _ = _mockAttachmentRepository.Received(1).GetAttachmentsByPropertyIdAsync(TestTenantId, TestPropertyId);
    }
    
    [Fact]
    public async Task GetAttachmentsForEntityAsync_WhenNoAttachmentsExist_ReturnsEmptyList()
    {
        // Arrange
        _mockAttachmentRepository.GetAttachmentsByPropertyIdAsync(TestTenantId, TestPropertyId)
            .Returns(new List<Attachment>());
        
        // Act
        var result = await _attachmentService.GetAttachmentsForEntityAsync(RentalAttachmentType.Property, TestPropertyId);
        
        // Assert
        Assert.Empty(result);
        
        _ = _mockAttachmentRepository.Received(1).GetAttachmentsByPropertyIdAsync(TestTenantId, TestPropertyId);
    }
    
    #endregion
    
    #region Helper Methods
    
    private IFormFile CreateMockFormFile(string fileName, string contentType, long length)
    {
        var content = new MemoryStream();
        var writer = new StreamWriter(content);
        writer.Write("Test file content");
        writer.Flush();
        content.Position = 0;
        
        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns(fileName);
        formFile.ContentType.Returns(contentType);
        formFile.Length.Returns(length);
        formFile.OpenReadStream().Returns(content);
        
        return formFile;
    }
    
    #endregion
}
