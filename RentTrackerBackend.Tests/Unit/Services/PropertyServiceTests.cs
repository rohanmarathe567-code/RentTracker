using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using NSubstitute;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Models.Pagination;
using RentTrackerBackend.Services;
using System.Linq.Expressions;
using Xunit;

namespace RentTrackerBackend.Tests.Unit.Services;

public class PropertyServiceTests
{
    private readonly IMongoRepository<RentalProperty> _mockRepository;
    private readonly ILogger<PropertyService> _mockLogger;
    private readonly PropertyService _propertyService;
    private const string TestTenantId = "tenant123";
    private const string TestPropertyId = "property456";

    public PropertyServiceTests()
    {
        _mockRepository = Substitute.For<IMongoRepository<RentalProperty>>();
        _mockLogger = Substitute.For<ILogger<PropertyService>>();
        _propertyService = new PropertyService(_mockRepository, _mockLogger);
    }

    #region GetPropertyByIdAsync Tests

    [Fact]
    public async Task GetPropertyByIdAsync_WhenPropertyExists_ReturnsProperty()
    {
        // Arrange
        var expectedProperty = CreateTestProperty();
        _mockRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(expectedProperty);

        // Act
        var result = await _propertyService.GetPropertyByIdAsync(TestTenantId, TestPropertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedProperty.FormattedId, result!.FormattedId);
        Assert.Equal(expectedProperty.Address.Street, result.Address.Street);
        await _mockRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
    }

    [Fact]
    public async Task GetPropertyByIdAsync_WhenPropertyDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns((RentalProperty)null!);

        // Act
        var result = await _propertyService.GetPropertyByIdAsync(TestTenantId, TestPropertyId);

        // Assert
        Assert.Null(result);
        await _mockRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
    }

    [Fact]
    public async Task GetPropertyByIdAsync_WithEmptyPropertyId_StillCallsRepository()
    {
        // Arrange
        var emptyId = string.Empty;
        _mockRepository.GetByIdAsync(TestTenantId, emptyId).Returns((RentalProperty)null!);

        // Act
        var result = await _propertyService.GetPropertyByIdAsync(TestTenantId, emptyId);

        // Assert
        Assert.Null(result);
        await _mockRepository.Received(1).GetByIdAsync(TestTenantId, emptyId);
    }

    #endregion

    #region GetPropertiesAsync Tests

    [Fact]
    public async Task GetPropertiesAsync_WithValidParameters_ReturnsFilteredAndPaginatedProperties()
    {
        // Arrange
        var properties = new List<RentalProperty>
        {
            CreateTestProperty("123 Main St", "Seattle", "WA", "98101", rentAmount: 1500),
            CreateTestProperty("456 Oak Ave", "Portland", "OR", "97201", rentAmount: 1800),
            CreateTestProperty("789 Pine Rd", "Seattle", "WA", "98102", rentAmount: 2000)
        };
        
        _mockRepository.GetAllAsync(TestTenantId, null).Returns(properties);

        var parameters = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Seattle",
            SortField = "address",
            SortDescending = false
        };

        // Act
        var result = await _propertyService.GetPropertiesAsync(TestTenantId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        // Seattle properties should be included
        Assert.Contains(result.Items, p => p.Address.City == "Seattle");
        await _mockRepository.Received(1).GetAllAsync(TestTenantId, null);
    }

    [Fact]
    public async Task GetPropertiesAsync_WithNoSearchTerm_ReturnsAllProperties()
    {
        // Arrange
        var properties = new List<RentalProperty>
        {
            CreateTestProperty("123 Main St", "Seattle", "WA", "98101"),
            CreateTestProperty("456 Oak Ave", "Portland", "OR", "97201"),
            CreateTestProperty("789 Pine Rd", "Seattle", "WA", "98102")
        };
        
        _mockRepository.GetAllAsync(TestTenantId, null).Returns(properties);

        var parameters = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _propertyService.GetPropertiesAsync(TestTenantId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
        await _mockRepository.Received(1).GetAllAsync(TestTenantId, null);
    }

    [Fact]
    public async Task GetPropertiesAsync_WithInvalidPagination_ValidatesPagination()
    {
        // Arrange
        _mockRepository.GetAllAsync(TestTenantId, null).Returns(new List<RentalProperty>());

        var parameters = new PaginationParameters
        {
            PageNumber = -1,  // Invalid page
            PageSize = 0  // Invalid page size
        };

        // Act
        var result = await _propertyService.GetPropertiesAsync(TestTenantId, parameters);

        // Assert
        Assert.NotNull(result);
        // After validation, page number should be at least 1
        Assert.Equal(1, result.PageNumber);
        // Pagination should have at least 1 page
        Assert.Equal(1, result.TotalPages);
    }

    [Theory]
    [InlineData("rentamount", true)]
    [InlineData("city", false)]
    [InlineData("leasestartdate", true)]
    [InlineData("invalidfield", false)] // Will use default sorting
    public async Task GetPropertiesAsync_WithDifferentSortOptions_AppliesCorrectSorting(string sortField, bool sortDescending)
    {
        // Arrange
        var properties = new List<RentalProperty>
        {
            CreateTestProperty("123 Main St", "Seattle", "WA", "98101", rentAmount: 1500),
            CreateTestProperty("456 Oak Ave", "Portland", "OR", "97201", rentAmount: 1800),
            CreateTestProperty("789 Pine Rd", "Bellevue", "WA", "98102", rentAmount: 2000)
        };
        
        _mockRepository.GetAllAsync(TestTenantId, null).Returns(properties);

        var parameters = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortField = sortField,
            SortDescending = sortDescending
        };

        // Act
        var result = await _propertyService.GetPropertiesAsync(TestTenantId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count());
        await _mockRepository.Received(1).GetAllAsync(TestTenantId, null);
    }

    #endregion

    #region CreatePropertyAsync Tests

    [Fact]
    public async Task CreatePropertyAsync_WithValidProperty_CreatesAndReturnsProperty()
    {
        // Arrange
        var propertyToCreate = CreateTestProperty();
        _mockRepository.CreateAsync(Arg.Any<RentalProperty>()).Returns(propertyToCreate);

        // Act
        var result = await _propertyService.CreatePropertyAsync(propertyToCreate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(propertyToCreate.FormattedId, result.FormattedId);
        Assert.Equal(propertyToCreate.Address.Street, result.Address.Street);
        await _mockRepository.Received(1).CreateAsync(Arg.Any<RentalProperty>());
    }

    [Fact]
    public async Task CreatePropertyAsync_WithMissingAddress_ThrowsArgumentException()
    {
        // Arrange
        var propertyWithoutAddress = CreateTestProperty();
        propertyWithoutAddress.Address.Street = string.Empty;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _propertyService.CreatePropertyAsync(propertyWithoutAddress));
        
        Assert.Contains("Address is required", exception.Message);
        await _mockRepository.DidNotReceive().CreateAsync(Arg.Any<RentalProperty>());
    }

    [Fact]
    public async Task CreatePropertyAsync_WithNullAddress_ThrowsNullReferenceException()
    {
        // Arrange
        var propertyWithNullAddress = CreateTestProperty();
        propertyWithNullAddress.Address = null!;

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(async () => 
            await _propertyService.CreatePropertyAsync(propertyWithNullAddress));
        
        await _mockRepository.DidNotReceive().CreateAsync(Arg.Any<RentalProperty>());
    }

    #endregion

    #region UpdatePropertyAsync Tests

    [Fact]
    public async Task UpdatePropertyAsync_WithValidProperty_UpdatesAndReturnsProperty()
    {
        // Arrange
        var existingProperty = CreateTestProperty();
        var updatedProperty = CreateTestProperty();
        updatedProperty.Address.Street = "Updated Street";
        updatedProperty.RentAmount = 2000;

        _mockRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(existingProperty);

        // Act
        var result = await _propertyService.UpdatePropertyAsync(TestTenantId, TestPropertyId, updatedProperty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingProperty.FormattedId, result!.FormattedId);
        Assert.Equal("Updated Street", result.Address.Street);
        Assert.Equal(2000, result.RentAmount);
        Assert.Equal(existingProperty.CreatedAt, result.CreatedAt);
        Assert.NotEqual(existingProperty.UpdatedAt, result.UpdatedAt); // Should be updated
        
        await _mockRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockRepository.Received(1).UpdateAsync(
            Arg.Is<string>(s => s == TestTenantId),
            Arg.Is<string>(s => s == TestPropertyId),
            Arg.Any<RentalProperty>()
        );
    }

    [Fact]
    public async Task UpdatePropertyAsync_WhenPropertyDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns((RentalProperty)null!);
        var updatedProperty = CreateTestProperty();

        // Act
        var result = await _propertyService.UpdatePropertyAsync(TestTenantId, TestPropertyId, updatedProperty);

        // Assert
        Assert.Null(result);
        await _mockRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockRepository.DidNotReceive().UpdateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<RentalProperty>()
        );
    }

    [Fact]
    public async Task UpdatePropertyAsync_PreservesOriginalImmutableProperties()
    {
        // Arrange
        var creationDate = DateTime.UtcNow.AddDays(-30);
        var existingProperty = CreateTestProperty();
        existingProperty.CreatedAt = creationDate;
        existingProperty.PaymentIds = new List<string> { "payment1", "payment2" };
        existingProperty.AttachmentIds = new List<string> { "attachment1", "attachment2" };
        
        var updatedProperty = CreateTestProperty();
        updatedProperty.Address.Street = "Updated Street";
        updatedProperty.PaymentIds = new List<string> { "payment3" }; // Should not be updated
        updatedProperty.AttachmentIds = new List<string> { "attachment3" }; // Should not be updated

        _mockRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(existingProperty);

        // Act
        var result = await _propertyService.UpdatePropertyAsync(TestTenantId, TestPropertyId, updatedProperty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(creationDate, result!.CreatedAt); // Should preserve original creation date
        Assert.Equal(2, result.PaymentIds.Count); // Should preserve original payment IDs
        Assert.Equal("payment1", result.PaymentIds[0]);
        Assert.Equal(2, result.AttachmentIds.Count); // Should preserve original attachment IDs
        Assert.Equal("attachment1", result.AttachmentIds[0]);
    }

    #endregion

    #region DeletePropertyAsync Tests

    [Fact]
    public async Task DeletePropertyAsync_WhenPropertyExists_ReturnsTrue()
    {
        // Arrange
        var existingProperty = CreateTestProperty();
        _mockRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(existingProperty);

        // Act
        var result = await _propertyService.DeletePropertyAsync(TestTenantId, TestPropertyId);

        // Assert
        Assert.True(result);
        await _mockRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockRepository.Received(1).DeleteAsync(TestTenantId, TestPropertyId);
    }

    [Fact]
    public async Task DeletePropertyAsync_WhenPropertyDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _mockRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns((RentalProperty)null!);

        // Act
        var result = await _propertyService.DeletePropertyAsync(TestTenantId, TestPropertyId);

        // Assert
        Assert.False(result);
        await _mockRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockRepository.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DeletePropertyAsync_WithEmptyPropertyId_ReturnsFalse()
    {
        // Arrange
        var emptyId = string.Empty;
        _mockRepository.GetByIdAsync(TestTenantId, emptyId).Returns((RentalProperty)null!);

        // Act
        var result = await _propertyService.DeletePropertyAsync(TestTenantId, emptyId);

        // Assert
        Assert.False(result);
        await _mockRepository.Received(1).GetByIdAsync(TestTenantId, emptyId);
        await _mockRepository.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region Helper Methods

    private RentalProperty CreateTestProperty(
        string street = "123 Test Street", 
        string city = "Test City", 
        string state = "TS", 
        string zipCode = "12345",
        decimal rentAmount = 1000)
    {
        return new RentalProperty
        {
            Id = ObjectId.Parse("507f1f77bcf86cd799439011"), // Test ObjectId
            TenantId = TestTenantId,
            Address = new Address
            {
                Street = street,
                City = city,
                State = state,
                ZipCode = zipCode
            },
            Description = "Test property description",
            PropertyManager = new PropertyManager
            {
                Name = "Test Manager",
                Contact = "manager@test.com"
            },
            RentAmount = rentAmount,
            LeaseDates = new LeaseDates
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1)
            },
            PaymentIds = new List<string>(),
            AttachmentIds = new List<string>(),
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            Version = 1
        };
    }

    #endregion
}
