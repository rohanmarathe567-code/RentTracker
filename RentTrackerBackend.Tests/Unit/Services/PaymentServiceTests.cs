using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Services;

namespace RentTrackerBackend.Tests.Unit.Services;

public class PaymentServiceTests
{
    private readonly IPaymentRepository _mockPaymentRepository;
    private readonly IMongoRepository<RentalProperty> _mockPropertyRepository;
    private readonly PaymentService _paymentService;
    private const string TestTenantId = "tenant123";
    private const string TestPropertyId = "property456";
    private const string TestPaymentId = "payment789";

    public PaymentServiceTests()
    {
        _mockPaymentRepository = Substitute.For<IPaymentRepository>();
        _mockPropertyRepository = Substitute.For<IMongoRepository<RentalProperty>>();
        _paymentService = new PaymentService(_mockPaymentRepository, _mockPropertyRepository);
    }

    #region GetPaymentByIdAsync Tests

    [Fact]
    public async Task GetPaymentByIdAsync_WhenPaymentExists_ReturnsPayment()
    {
        // Arrange
        var expectedPayment = CreateTestPayment();
        _mockPaymentRepository.GetByIdAsync(TestTenantId, TestPaymentId).Returns(expectedPayment);

        // Act
        var result = await _paymentService.GetPaymentByIdAsync(TestTenantId, TestPaymentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPayment.FormattedId, result!.FormattedId);
        Assert.Equal(expectedPayment.Amount, result.Amount);
        await _mockPaymentRepository.Received(1).GetByIdAsync(TestTenantId, TestPaymentId);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WhenPaymentDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockPaymentRepository.GetByIdAsync(TestTenantId, TestPaymentId).Returns((RentalPayment)null!);

        // Act
        var result = await _paymentService.GetPaymentByIdAsync(TestTenantId, TestPaymentId);

        // Assert
        Assert.Null(result);
        await _mockPaymentRepository.Received(1).GetByIdAsync(TestTenantId, TestPaymentId);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WithEmptyPaymentId_StillCallsRepository()
    {
        // Arrange
        var emptyId = string.Empty;
        _mockPaymentRepository.GetByIdAsync(TestTenantId, emptyId).Returns((RentalPayment)null!);

        // Act
        var result = await _paymentService.GetPaymentByIdAsync(TestTenantId, emptyId);

        // Assert
        Assert.Null(result);
        await _mockPaymentRepository.Received(1).GetByIdAsync(TestTenantId, emptyId);
    }

    #endregion

    #region GetPaymentsByPropertyAsync Tests

    [Fact]
    public async Task GetPaymentsByPropertyAsync_WhenPropertyExists_ReturnsPayments()
    {
        // Arrange
        var property = CreateTestProperty();
        var payments = new List<RentalPayment>
        {
            CreateTestPayment(propertyId: TestPropertyId, amount: 1000),
            CreateTestPayment(propertyId: TestPropertyId, amount: 1500),
            CreateTestPayment(propertyId: "differentProperty", amount: 2000)
        };

        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(property);
        _mockPaymentRepository.GetAllAsync(TestTenantId, null).Returns(payments);

        // Act
        var result = await _paymentService.GetPaymentsByPropertyAsync(TestTenantId, TestPropertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, payment => Assert.Equal(TestPropertyId, payment.RentalPropertyId));
        await _mockPropertyRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockPaymentRepository.Received(1).GetAllAsync(TestTenantId, null);
    }

    [Fact]
    public async Task GetPaymentsByPropertyAsync_WhenPropertyDoesNotExist_ThrowsArgumentException()
    {
        // Arrange
        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns((RentalProperty)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _paymentService.GetPaymentsByPropertyAsync(TestTenantId, TestPropertyId));
        
        Assert.Contains(TestPropertyId, exception.Message);
        await _mockPropertyRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockPaymentRepository.DidNotReceive().GetAllAsync(Arg.Any<string>(), Arg.Any<string[]>());
    }

    [Fact]
    public async Task GetPaymentsByPropertyAsync_WithIncludes_PassesIncludesToRepository()
    {
        // Arrange
        var property = CreateTestProperty();
        var payments = new List<RentalPayment> { CreateTestPayment(propertyId: TestPropertyId) };
        var includes = new[] { "PaymentMethod" };

        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(property);
        _mockPaymentRepository.GetAllAsync(TestTenantId, includes).Returns(payments);

        // Act
        var result = await _paymentService.GetPaymentsByPropertyAsync(TestTenantId, TestPropertyId, includes);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        await _mockPropertyRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockPaymentRepository.Received(1).GetAllAsync(TestTenantId, includes);
    }

    [Fact]
    public async Task GetPaymentsByPropertyAsync_WhenNoPaymentsExistForProperty_ReturnsEmptyList()
    {
        // Arrange
        var property = CreateTestProperty();
        var payments = new List<RentalPayment>
        {
            CreateTestPayment(propertyId: "differentProperty", amount: 2000)
        };

        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(property);
        _mockPaymentRepository.GetAllAsync(TestTenantId, null).Returns(payments);

        // Act
        var result = await _paymentService.GetPaymentsByPropertyAsync(TestTenantId, TestPropertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        await _mockPropertyRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockPaymentRepository.Received(1).GetAllAsync(TestTenantId, null);
    }

    #endregion

    #region CreatePaymentAsync Tests

    [Fact]
    public async Task CreatePaymentAsync_WithValidPayment_CreatesAndReturnsPayment()
    {
        // Arrange
        var property = CreateTestProperty();
        var paymentToCreate = CreateTestPayment();
        
        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(property);
        _mockPaymentRepository.CreateAsync(Arg.Any<RentalPayment>()).Returns(paymentToCreate);

        // Act
        var result = await _paymentService.CreatePaymentAsync(paymentToCreate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(paymentToCreate.FormattedId, result.FormattedId);
        Assert.Equal(paymentToCreate.Amount, result.Amount);
        await _mockPropertyRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockPaymentRepository.Received(1).CreateAsync(Arg.Any<RentalPayment>());
    }

    [Fact]
    public async Task CreatePaymentAsync_WithNonUtcDate_ConvertsToUtc()
    {
        // Arrange
        var property = CreateTestProperty();
        var paymentWithLocalDate = CreateTestPayment();
        paymentWithLocalDate.PaymentDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
        
        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(property);
        
        _mockPaymentRepository.CreateAsync(Arg.Do<RentalPayment>(payment => 
            Assert.Equal(DateTimeKind.Utc, payment.PaymentDate.Kind)))
            .Returns(paymentWithLocalDate);

        // Act
        var result = await _paymentService.CreatePaymentAsync(paymentWithLocalDate);

        // Assert
        Assert.Equal(DateTimeKind.Utc, result.PaymentDate.Kind);
    }

    [Fact]
    public async Task CreatePaymentAsync_WithUtcDate_MaintainsUtc()
    {
        // Arrange
        var property = CreateTestProperty();
        var paymentWithUtcDate = CreateTestPayment();
        paymentWithUtcDate.PaymentDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        
        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns(property);
        
        _mockPaymentRepository.CreateAsync(Arg.Do<RentalPayment>(payment => 
            Assert.Equal(DateTimeKind.Utc, payment.PaymentDate.Kind)))
            .Returns(paymentWithUtcDate);

        // Act
        var result = await _paymentService.CreatePaymentAsync(paymentWithUtcDate);

        // Assert
        Assert.Equal(DateTimeKind.Utc, result.PaymentDate.Kind);
    }

    [Fact]
    public async Task CreatePaymentAsync_WhenPropertyDoesNotExist_ThrowsArgumentException()
    {
        // Arrange
        var paymentToCreate = CreateTestPayment();
        _mockPropertyRepository.GetByIdAsync(TestTenantId, TestPropertyId).Returns((RentalProperty)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _paymentService.CreatePaymentAsync(paymentToCreate));
        
        Assert.Contains(TestPropertyId, exception.Message);
        await _mockPropertyRepository.Received(1).GetByIdAsync(TestTenantId, TestPropertyId);
        await _mockPaymentRepository.DidNotReceive().CreateAsync(Arg.Any<RentalPayment>());
    }

    #endregion

    #region UpdatePaymentAsync Tests

    [Fact]
    public async Task UpdatePaymentAsync_WithValidPayment_UpdatesAndReturnsPayment()
    {
        // Arrange
        var existingPayment = CreateTestPayment(amount: 1000);
        var updatedPayment = CreateTestPayment(amount: 1500);
        updatedPayment.PaymentReference = "Updated Reference";
        updatedPayment.Notes = "Updated Notes";

        _mockPaymentRepository.GetByIdAsync(TestTenantId, TestPaymentId).Returns(existingPayment);

        // Act
        var result = await _paymentService.UpdatePaymentAsync(TestTenantId, TestPaymentId, updatedPayment);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingPayment.FormattedId, result!.FormattedId);
        Assert.Equal(1500, result.Amount);
        Assert.Equal("Updated Reference", result.PaymentReference);
        Assert.Equal("Updated Notes", result.Notes);
        
        await _mockPaymentRepository.Received(1).GetByIdAsync(TestTenantId, TestPaymentId);
        await _mockPaymentRepository.Received(1).UpdateAsync(
            Arg.Is<string>(s => s == TestTenantId),
            Arg.Is<string>(s => s == TestPaymentId),
            Arg.Any<RentalPayment>()
        );
    }

    [Fact]
    public async Task UpdatePaymentAsync_WhenPaymentDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockPaymentRepository.GetByIdAsync(TestTenantId, TestPaymentId).Returns((RentalPayment)null!);
        var updatedPayment = CreateTestPayment();

        // Act
        var result = await _paymentService.UpdatePaymentAsync(TestTenantId, TestPaymentId, updatedPayment);

        // Assert
        Assert.Null(result);
        await _mockPaymentRepository.Received(1).GetByIdAsync(TestTenantId, TestPaymentId);
        await _mockPaymentRepository.DidNotReceive().UpdateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<RentalPayment>()
        );
    }

    [Fact]
    public async Task UpdatePaymentAsync_WithNonUtcDate_ConvertsToUtc()
    {
        // Arrange
        var existingPayment = CreateTestPayment();
        var updatedPayment = CreateTestPayment();
        updatedPayment.PaymentDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);

        _mockPaymentRepository.GetByIdAsync(TestTenantId, TestPaymentId).Returns(existingPayment);

        // Act
        var result = await _paymentService.UpdatePaymentAsync(TestTenantId, TestPaymentId, updatedPayment);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.PaymentDate.Kind);
    }

    #endregion

    #region DeletePaymentAsync Tests

    [Fact]
    public async Task DeletePaymentAsync_WhenSuccessful_ReturnsTrue()
    {
        // Act
        var result = await _paymentService.DeletePaymentAsync(TestTenantId, TestPaymentId);

        // Assert
        Assert.True(result);
        await _mockPaymentRepository.Received(1).DeleteAsync(TestTenantId, TestPaymentId);
    }

    [Fact]
    public async Task DeletePaymentAsync_WhenExceptionThrown_ReturnsFalse()
    {
        // Arrange
        _mockPaymentRepository.DeleteAsync(TestTenantId, TestPaymentId)
            .Throws(new Exception("Failed to delete payment"));

        // Act
        var result = await _paymentService.DeletePaymentAsync(TestTenantId, TestPaymentId);

        // Assert
        Assert.False(result);
        await _mockPaymentRepository.Received(1).DeleteAsync(TestTenantId, TestPaymentId);
    }

    [Fact]
    public async Task DeletePaymentAsync_WithEmptyPaymentId_StillCallsRepository()
    {
        // Arrange
        var emptyId = string.Empty;

        // Act
        var result = await _paymentService.DeletePaymentAsync(TestTenantId, emptyId);

        // Assert
        Assert.True(result);
        await _mockPaymentRepository.Received(1).DeleteAsync(TestTenantId, emptyId);
    }

    #endregion

    #region Helper Methods

    private RentalPayment CreateTestPayment(decimal amount = 1000, string? propertyId = null)
    {
        return new RentalPayment
        {
            Id = ObjectId.Parse("507f1f77bcf86cd799439011"), // Test ObjectId
            TenantId = TestTenantId,
            RentalPropertyId = propertyId ?? TestPropertyId,
            Amount = amount,
            PaymentDate = DateTime.UtcNow.AddDays(-1),
            PaymentMethodId = "method123",
            PaymentReference = "REF12345",
            Notes = "Test payment notes",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };
    }

    private RentalProperty CreateTestProperty()
    {
        return new RentalProperty
        {
            Id = ObjectId.Parse("607f1f77bcf86cd799439022"), // Test ObjectId
            TenantId = TestTenantId,
            Address = new Address
            {
                Street = "123 Test Street",
                City = "Test City",
                State = "TS",
                ZipCode = "12345"
            },
            Description = "Test property description",
            PropertyManager = new PropertyManager
            {
                Name = "Test Manager",
                Contact = "manager@test.com"
            },
            RentAmount = 1000,
            LeaseDates = new LeaseDates
            {
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow.AddMonths(11)
            },
            PaymentIds = new List<string>(),
            AttachmentIds = new List<string>()
        };
    }

    #endregion
}
