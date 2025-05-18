using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Services;
using Xunit;

namespace RentTrackerBackend.Tests.Unit.Services
{
    public class PropertyTransactionServiceTests
    {
        private readonly IPropertyTransactionRepository _transactionRepository;
        private readonly IMongoRepository<RentalProperty> _propertyRepository;
        private readonly ITransactionCategoryRepository _categoryRepository;
        private readonly PropertyTransactionService _service;

        public PropertyTransactionServiceTests()
        {
            _transactionRepository = Substitute.For<IPropertyTransactionRepository>();
            _propertyRepository = Substitute.For<IMongoRepository<RentalProperty>>();
            _categoryRepository = Substitute.For<ITransactionCategoryRepository>();
            _service = new PropertyTransactionService(
                _transactionRepository,
                _propertyRepository,
                _categoryRepository);
        }

        [Fact]
        public async Task GetTransactionByIdAsync_ShouldReturnTransaction_WhenTransactionExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var transactionId = ObjectId.GenerateNewId().ToString();
            var expectedTransaction = new PropertyTransaction { TenantId = tenantId };

            _transactionRepository.GetByIdAsync(tenantId, transactionId)
                .Returns(expectedTransaction);

            // Act
            var result = await _service.GetTransactionByIdAsync(tenantId, transactionId);

            // Assert
            result.Should().BeEquivalentTo(expectedTransaction);
        }

        [Fact]
        public async Task GetTransactionsByPropertyAsync_ShouldReturnTransactions_WhenPropertyExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();
            var property = new RentalProperty { TenantId = tenantId };
            var expectedTransactions = new List<PropertyTransaction>
            {
                new PropertyTransaction { TenantId = tenantId, RentalPropertyId = propertyId },
                new PropertyTransaction { TenantId = tenantId, RentalPropertyId = propertyId }
            };

            _propertyRepository.GetByIdAsync(tenantId, propertyId)
                .Returns(property);
            _transactionRepository.GetAllAsync(tenantId, true, null)
                .Returns(expectedTransactions);

            // Act
            var result = await _service.GetTransactionsByPropertyAsync(tenantId, propertyId);

            // Assert
            result.Should().BeEquivalentTo(expectedTransactions);
        }

        [Fact]
        public async Task GetTransactionsByPropertyAsync_ShouldThrowArgumentException_WhenPropertyDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();

            _propertyRepository.GetByIdAsync(tenantId, propertyId)
                .Returns(Task.FromResult((RentalProperty)null!));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.GetTransactionsByPropertyAsync(tenantId, propertyId));
        }

        [Fact]
        public async Task CreateTransactionAsync_ShouldCreateTransaction_WhenValidInputProvided()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();
            var categoryId = ObjectId.GenerateNewId().ToString();
            var transaction = new PropertyTransaction
            {
                TenantId = tenantId,
                RentalPropertyId = propertyId,
                CategoryId = categoryId,
                Amount = 100,
                TransactionDate = DateTime.UtcNow,
                TransactionType = TransactionType.Income
            };

            var property = new RentalProperty { TenantId = tenantId };
            var category = new PropertyTransactionCategory 
            { 
                TransactionType = TransactionType.Income 
            };

            _propertyRepository.GetByIdAsync(tenantId, propertyId)
                .Returns(property);
            _categoryRepository.GetSharedByIdAsync(categoryId)
                .Returns(category);
            _transactionRepository.CreateAsync(transaction)
                .Returns(transaction);

            // Act
            var result = await _service.CreateTransactionAsync(transaction);

            // Assert
            result.Should().BeEquivalentTo(transaction);
            await _transactionRepository.Received(1).CreateAsync(transaction);
        }

        [Fact]
        public async Task CreateTransactionAsync_ShouldThrowArgumentException_WhenCategoryNotFound()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();
            var categoryId = ObjectId.GenerateNewId().ToString();
            var transaction = new PropertyTransaction
            {
                TenantId = tenantId,
                RentalPropertyId = propertyId,
                CategoryId = categoryId
            };

            var property = new RentalProperty { TenantId = tenantId };

            _propertyRepository.GetByIdAsync(tenantId, propertyId)
                .Returns(property);
            _categoryRepository.GetSharedByIdAsync(categoryId)
                .Returns(Task.FromResult<PropertyTransactionCategory?>(null));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateTransactionAsync(transaction));
        }

        [Fact]
        public async Task CreateTransactionAsync_ShouldThrowArgumentException_WhenTransactionTypeMismatch()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();
            var categoryId = ObjectId.GenerateNewId().ToString();
            var transaction = new PropertyTransaction
            {
                TenantId = tenantId,
                RentalPropertyId = propertyId,
                CategoryId = categoryId,
                TransactionType = TransactionType.Income
            };

            var property = new RentalProperty { TenantId = tenantId };
            var category = new PropertyTransactionCategory 
            { 
                TransactionType = TransactionType.Expense 
            };

            _propertyRepository.GetByIdAsync(tenantId, propertyId)
                .Returns(property);
            _categoryRepository.GetSharedByIdAsync(categoryId)
                .Returns(category);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateTransactionAsync(transaction));
        }

        [Fact]
        public async Task UpdateTransactionAsync_ShouldUpdateTransaction_WhenTransactionExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var transactionId = ObjectId.GenerateNewId().ToString();
            var existingTransaction = new PropertyTransaction
            {
                TenantId = tenantId,
                Amount = 100,
                TransactionDate = DateTime.UtcNow
            };
            var updatedTransaction = new PropertyTransaction
            {
                TenantId = tenantId,
                Amount = 200,
                TransactionDate = DateTime.UtcNow
            };

            _transactionRepository.GetByIdAsync(tenantId, transactionId)
                .Returns(existingTransaction);

            // Act
            var result = await _service.UpdateTransactionAsync(tenantId, transactionId, updatedTransaction);

            // Assert
            result.Should().NotBeNull();
            result.Amount.Should().Be(updatedTransaction.Amount);
            await _transactionRepository.Received(1).UpdateAsync(tenantId, transactionId, Arg.Any<PropertyTransaction>());
        }

        [Fact]
        public async Task UpdateTransactionAsync_ShouldReturnNull_WhenTransactionDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var transactionId = ObjectId.GenerateNewId().ToString();
            var updatedTransaction = new PropertyTransaction();

            _transactionRepository.GetByIdAsync(tenantId, transactionId)
                .Returns(Task.FromResult<PropertyTransaction?>(null));

            // Act
            var result = await _service.UpdateTransactionAsync(tenantId, transactionId, updatedTransaction);

            // Assert
            result.Should().BeNull();
            await _transactionRepository.DidNotReceive().UpdateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<PropertyTransaction>());
        }

        [Fact]
        public async Task GetTotalIncomeByPropertyAsync_ShouldCalculateCorrectTotal()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();
            var transactions = new List<PropertyTransaction>
            {
                new PropertyTransaction { TenantId = tenantId, RentalPropertyId = propertyId, TransactionType = TransactionType.Income, Amount = 100 },
                new PropertyTransaction { TenantId = tenantId, RentalPropertyId = propertyId, TransactionType = TransactionType.Income, Amount = 200 },
                new PropertyTransaction { TenantId = tenantId, RentalPropertyId = propertyId, TransactionType = TransactionType.Expense, Amount = 50 }
            };

            var property = new RentalProperty { TenantId = tenantId };

            _propertyRepository.GetByIdAsync(tenantId, propertyId)
                .Returns(property);
            _transactionRepository.GetAllAsync(tenantId, true, null)
                .Returns(transactions);

            // Act
            var result = await _service.GetTotalIncomeByPropertyAsync(tenantId, propertyId);

            // Assert
            result.Should().Be(300);
        }

        [Fact]
        public async Task GetTotalExpensesByPropertyAsync_ShouldCalculateCorrectTotal()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();
            var transactions = new List<PropertyTransaction>
            {
                new PropertyTransaction { TenantId = tenantId, RentalPropertyId = propertyId, TransactionType = TransactionType.Expense, Amount = 100 },
                new PropertyTransaction { TenantId = tenantId, RentalPropertyId = propertyId, TransactionType = TransactionType.Expense, Amount = 200 },
                new PropertyTransaction { TenantId = tenantId, RentalPropertyId = propertyId, TransactionType = TransactionType.Income, Amount = 500 }
            };

            var property = new RentalProperty { TenantId = tenantId };

            _propertyRepository.GetByIdAsync(tenantId, propertyId)
                .Returns(property);
            _transactionRepository.GetAllAsync(tenantId, true, null)
                .Returns(transactions);

            // Act
            var result = await _service.GetTotalExpensesByPropertyAsync(tenantId, propertyId);

            // Assert
            result.Should().Be(300);
        }

        [Fact]
        public async Task GetTransactionsByCategoryAsync_ShouldReturnCorrectGrouping()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();
            var transactions = new List<PropertyTransaction>
            {
                new PropertyTransaction 
                { 
                    TenantId = tenantId, 
                    RentalPropertyId = propertyId, 
                    TransactionType = TransactionType.Income,
                    Amount = 100,
                    Category = new PropertyTransactionCategory { Name = "Rent" }
                },
                new PropertyTransaction 
                { 
                    TenantId = tenantId, 
                    RentalPropertyId = propertyId, 
                    TransactionType = TransactionType.Income,
                    Amount = 200,
                    Category = new PropertyTransactionCategory { Name = "Rent" }
                },
                new PropertyTransaction 
                { 
                    TenantId = tenantId, 
                    RentalPropertyId = propertyId, 
                    TransactionType = TransactionType.Income,
                    Amount = 150,
                    Category = new PropertyTransactionCategory { Name = "Deposit" }
                }
            };

            var property = new RentalProperty { TenantId = tenantId };

            _propertyRepository.GetByIdAsync(tenantId, propertyId)
                .Returns(property);
            _transactionRepository.GetAllAsync(tenantId, true, Arg.Any<string[]>())
                .Returns(transactions);

            // Act
            var result = await _service.GetTransactionsByCategoryAsync(tenantId, propertyId, TransactionType.Income);

            // Assert
            result.Should().ContainKey("Rent").WhoseValue.Should().Be(300);
            result.Should().ContainKey("Deposit").WhoseValue.Should().Be(150);
        }

        [Fact]
        public async Task GetTransactionsByTypeAsync_ShouldFilterByDateRange()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 12, 31);
            var transactions = new List<PropertyTransaction>
            {
                new PropertyTransaction 
                { 
                    TenantId = tenantId, 
                    RentalPropertyId = propertyId, 
                    TransactionType = TransactionType.Income,
                    Amount = 100,
                    TransactionDate = new DateTime(2025, 6, 1)
                },
                new PropertyTransaction 
                { 
                    TenantId = tenantId, 
                    RentalPropertyId = propertyId, 
                    TransactionType = TransactionType.Income,
                    Amount = 200,
                    TransactionDate = new DateTime(2024, 12, 31)
                }
            };

            var property = new RentalProperty { TenantId = tenantId };

            _propertyRepository.GetByIdAsync(tenantId, propertyId)
                .Returns(property);
            _transactionRepository.GetAllAsync(tenantId, true, Arg.Any<string[]>())
                .Returns(transactions);

            // Act
            var result = await _service.GetTransactionsByTypeAsync(tenantId, propertyId, TransactionType.Income, startDate, endDate);

            // Assert
            result.Should().HaveCount(1);
            result.First().Amount.Should().Be(100);
        }

        [Fact]
        public async Task DeleteTransactionAsync_ShouldReturnTrue_WhenTransactionIsDeleted()
        {
            // Arrange
            var tenantId = "tenant123";
            var transactionId = ObjectId.GenerateNewId().ToString();

            // Act
            var result = await _service.DeleteTransactionAsync(tenantId, transactionId);

            // Assert
            result.Should().BeTrue();
            await _transactionRepository.Received(1).DeleteAsync(tenantId, transactionId);
        }

        [Fact]
        public async Task DeleteTransactionAsync_ShouldReturnFalse_WhenExceptionOccurs()
        {
            // Arrange
            var tenantId = "tenant123";
            var transactionId = ObjectId.GenerateNewId().ToString();

            _transactionRepository.DeleteAsync(tenantId, transactionId)
                .ThrowsAsync(new Exception());

            // Act
            var result = await _service.DeleteTransactionAsync(tenantId, transactionId);

            // Assert
            result.Should().BeFalse();
        }

   }
}