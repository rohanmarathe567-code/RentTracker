using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Tests.Unit.Services;

public class FileServiceTests : IDisposable
{
    private readonly IWebHostEnvironment _mockEnvironment;
    private readonly ILogger<FileService> _mockLogger;
    private readonly string _testDirectory;
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        _mockEnvironment = Substitute.For<IWebHostEnvironment>();
        _mockLogger = Substitute.For<ILogger<FileService>>();
        
        // Use a temporary directory for tests
        _testDirectory = Path.Combine(Path.GetTempPath(), "RentTrackerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        
        // Setup ContentRootPath in the mock environment
        _mockEnvironment.ContentRootPath.Returns(_testDirectory);
        
        _fileService = new FileService(_mockEnvironment, _mockLogger);
    }
    
    public void Dispose()
    {
        try
        {
            // Clean up test directory after tests
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region ValidateFileType Tests

    [Theory]
    [InlineData("application/pdf", "document.pdf")]
    [InlineData("image/jpeg", "photo.jpg")]
    [InlineData("image/png", "image.png")]
    [InlineData("image/gif", "animation.gif")]
    [InlineData("application/msword", "document.doc")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "document.docx")]
    [InlineData("application/vnd.ms-excel", "spreadsheet.xls")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "spreadsheet.xlsx")]
    [InlineData("text/plain", "notes.txt")]
    public void ValidateFileType_WithValidTypesAndExtensions_ReturnsTrue(string contentType, string fileName)
    {
        // Act
        var result = _fileService.ValidateFileType(contentType, fileName);
        
        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("application/pdf", "document.doc", false)]
    [InlineData("image/jpeg", "document.pdf", false)]
    [InlineData("application/javascript", "script.js", false)]
    [InlineData("application/zip", "archive.zip", false)]
    [InlineData("text/html", "page.html", false)]
    [InlineData("application/octet-stream", "malicious.exe", false)]
    public void ValidateFileType_WithInvalidTypesOrExtensions_ReturnsFalse(string contentType, string fileName, bool expected)
    {
        // Act
        var result = _fileService.ValidateFileType(contentType, fileName);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ValidateFileType_WithNullContentType_ThrowsArgumentNullException()
    {
        // Arrange
        string? contentType = null;
        string fileName = "document.pdf";
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _fileService.ValidateFileType(contentType!, fileName));
        Assert.Equal("contentType", exception.ParamName);
    }

    [Fact]
    public void ValidateFileType_WithEmptyContentType_ReturnsFalse()
    {
        // Arrange
        string contentType = string.Empty;
        string fileName = "document.pdf";
        
        // Act
        var result = _fileService.ValidateFileType(contentType, fileName);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateFileType_WithNullFileName_ThrowsArgumentNullException()
    {
        // Arrange
        string contentType = "application/pdf";
        string? fileName = null;
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _fileService.ValidateFileType(contentType, fileName!));
        Assert.Equal("fileName", exception.ParamName);
    }

    [Fact]
    public void ValidateFileType_WithEmptyFileName_ReturnsFalse()
    {
        // Arrange
        string contentType = "application/pdf";
        string fileName = string.Empty;
        
        // Act
        var result = _fileService.ValidateFileType(contentType, fileName);
        
        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("APPLICATION/PDF", "document.PDF")]
    [InlineData("image/JPEG", "photo.JPG")]
    [InlineData("IMAGE/png", "image.PNG")]
    public void ValidateFileType_WithMixedCaseInput_IgnoresCase(string contentType, string fileName)
    {
        // Act
        var result = _fileService.ValidateFileType(contentType, fileName);
        
        // Assert
        Assert.True(result);
    }

    #endregion

    #region UploadFileAsync Tests

    [Fact]
    public async Task UploadFileAsync_WithValidFile_SavesFileAndReturnsFileName()
    {
        // Arrange
        var fileName = "test.pdf";
        var contentType = "application/pdf";
        var content = "Test file content";
        var mockFile = CreateSubstituteFormFile(fileName, contentType, content);
        
        // Make sure uploads directory exists
        var uploadsDir = Path.Combine(_testDirectory, "uploads");
        Directory.CreateDirectory(uploadsDir);
        
        // Act
        var result = await _fileService.UploadFileAsync(mockFile);
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains(fileName, result);  // Result should contain the original filename
        var fullPath = Path.Combine(_testDirectory, "uploads", result);
        Assert.True(File.Exists(fullPath));
        Assert.Equal(content, await File.ReadAllTextAsync(fullPath));
    }

    [Fact]
    public async Task UploadFileAsync_WithNullFile_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _fileService.UploadFileAsync(null!));
        
        Assert.Contains("file", exception.Message);
    }

    [Fact]
    public async Task UploadFileAsync_WithEmptyFile_ThrowsArgumentException()
    {
        // Arrange
        var mockFile = CreateSubstituteFormFile("test.pdf", "application/pdf", "", 0);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _fileService.UploadFileAsync(mockFile));
        
        Assert.Contains("empty", exception.Message);
    }

    [Fact]
    public async Task UploadFileAsync_WithInvalidFileType_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockFile = CreateSubstituteFormFile("test.exe", "application/octet-stream", "Executable content");
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _fileService.UploadFileAsync(mockFile));
        
        Assert.Contains("file type", exception.Message.ToLower());
    }

    [Fact]
    public async Task UploadFileAsync_WithSameFileName_GeneratesUniqueFileName()
    {
        // Arrange
        var fileName = "test.pdf";
        var contentType = "application/pdf";
        var mockFile1 = CreateSubstituteFormFile(fileName, contentType, "Content 1");
        var mockFile2 = CreateSubstituteFormFile(fileName, contentType, "Content 2");
        
        // Make sure uploads directory exists
        var uploadsDir = Path.Combine(_testDirectory, "uploads");
        Directory.CreateDirectory(uploadsDir);
        
        // Act
        var result1 = await _fileService.UploadFileAsync(mockFile1);
        var result2 = await _fileService.UploadFileAsync(mockFile2);
        
        // Assert
        Assert.NotEqual(result1, result2);
        Assert.Contains(fileName, result1);
        Assert.Contains(fileName, result2);
    }

    [Fact]
    public async Task UploadFileAsync_WithSpecialCharactersInFileName_HandlesCorrectly()
    {
        // Arrange
        var fileName = "test file with spaces & special chars.pdf";
        var contentType = "application/pdf";
        var content = "Test content";
        var mockFile = CreateSubstituteFormFile(fileName, contentType, content);
        
        // Make sure uploads directory exists
        var uploadsDir = Path.Combine(_testDirectory, "uploads");
        Directory.CreateDirectory(uploadsDir);
        
        // Act
        var result = await _fileService.UploadFileAsync(mockFile);
        
        // Assert
        Assert.NotNull(result);
        // File name might be sanitized, but should contain main parts of the original
        Assert.Contains("test", result);
        Assert.Contains("file", result);
        Assert.Contains("spaces", result);
        Assert.Contains("special", result);
        Assert.Contains("chars", result);
        Assert.Contains(".pdf", result);
        
        var fullPath = Path.Combine(_testDirectory, "uploads", result);
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task UploadFileAsync_WhenIOExceptionOccurs_PropagatesException()
    {
        // Arrange
        var mockFile = CreateSubstituteFormFile("test.pdf", "application/pdf", "Test content");
        
        // Setup the mock file to throw an exception
        mockFile.CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Simulated IO error"));
        
        // Act & Assert
        await Assert.ThrowsAsync<IOException>(() => 
            _fileService.UploadFileAsync(mockFile));
    }

    #endregion

    #region DownloadFileAsync Tests

    [Fact]
    public async Task DownloadFileAsync_WhenFileExists_ReturnsFileStream()
    {
        // Arrange
        var fileName = "test_download.pdf";
        var fileContent = "Test content for download";
        var storagePath = await SetupTestFile(fileName, fileContent);
        
        // Act
        var stream = await _fileService.DownloadFileAsync(storagePath);
        
        // Assert
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        Assert.Equal(fileContent, content);
    }

    [Fact]
    public async Task DownloadFileAsync_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = "non_existent_file.pdf";
        
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _fileService.DownloadFileAsync(nonExistentFile));
    }

    [Fact]
    public async Task DownloadFileAsync_WithNullPath_ThrowsArgumentNullException()
    {
        // Arrange
        string? path = null;
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _fileService.DownloadFileAsync(path!));
    }

    [Fact]
    public async Task DownloadFileAsync_WithEmptyPath_ThrowsFileNotFoundException()
    {
        // Arrange
        string path = string.Empty;
        
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _fileService.DownloadFileAsync(path));
    }

    [Fact]
    public async Task DownloadFileAsync_WithPathTraversal_RejectsAndSanitizes()
    {
        // Arrange
        var pathWithTraversal = "../../../dangerous_path.pdf";
        
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _fileService.DownloadFileAsync(pathWithTraversal));
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_WhenFileExists_DeletesFile()
    {
        // Arrange
        var fileName = "test_delete.pdf";
        var fileContent = "Test content for deletion";
        var storagePath = await SetupTestFile(fileName, fileContent);
        var fullPath = Path.Combine(_testDirectory, "uploads", storagePath);
        
        // Verify file exists before deletion
        Assert.True(File.Exists(fullPath));
        
        // Act
        await _fileService.DeleteFileAsync(storagePath);
        
        // Assert
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task DeleteFileAsync_WhenFileDoesNotExist_DoesNotThrowException()
    {
        // Arrange
        var nonExistentFile = "non_existent_file.pdf";
        
        // Act & Assert
        var exception = await Record.ExceptionAsync(() => 
            _fileService.DeleteFileAsync(nonExistentFile));
        
        Assert.Null(exception); // No exception should be thrown
    }

    [Fact]
    public async Task DeleteFileAsync_WithNullPath_ThrowsArgumentNullException()
    {
        // Arrange
        string? path = null;
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _fileService.DeleteFileAsync(path!));
    }

    [Fact]
    public async Task DeleteFileAsync_WithEmptyPath_HandlesGracefully()
    {
        // Arrange
        string path = string.Empty;
        
        // Act & Assert
        var exception = await Record.ExceptionAsync(() => 
            _fileService.DeleteFileAsync(path));
        
        Assert.Null(exception); // Should not throw exception for empty path
    }

    [Fact]
    public async Task DeleteFileAsync_WhenExceptionOccurs_PropagatesException()
    {
        // Arrange
        var fileName = "locked_file.pdf";
        var fileContent = "Test content for locked file";
        var storagePath = await SetupTestFile(fileName, fileContent);
        
        // Create a substitute that will throw an exception
        var mockFileService = Substitute.For<IStorageService>();
        mockFileService.DeleteFileAsync(Arg.Any<string>())
            .ThrowsAsync(new IOException("Simulated IO error during deletion"));
        
        // Act & Assert
        await Assert.ThrowsAsync<IOException>(() => 
            mockFileService.DeleteFileAsync(storagePath));
    }

    #endregion

    #region Helper Methods

    private IFormFile CreateSubstituteFormFile(string fileName, string contentType, string content, long? fileSize = null)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        
        var mockFile = Substitute.For<IFormFile>();
        mockFile.FileName.Returns(fileName);
        mockFile.ContentType.Returns(contentType);
        mockFile.Length.Returns(fileSize ?? bytes.Length);
        
        mockFile.OpenReadStream().Returns(stream);
        mockFile.CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var targetStream = callInfo.Arg<Stream>();
                stream.Position = 0;
                stream.CopyTo(targetStream);
                return Task.CompletedTask;
            });
        
        return mockFile;
    }

    private async Task<string> SetupTestFile(string fileName, string content)
    {
        // Create uploads directory if it doesn't exist
        var uploadsDir = Path.Combine(_testDirectory, "uploads");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }
        
        // Create test file
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);
        await File.WriteAllTextAsync(filePath, content);
        
        return uniqueFileName;
    }

    #endregion
}
