using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Tests.Unit.Repositories
{
    public class TransactionCategoryRepositoryTests
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<PropertyTransactionCategory> _collection;
        private readonly TransactionCategoryRepository _repository;

        public TransactionCategoryRepositoryTests()
        {
            // Create substitutes
            _database = Substitute.For<IMongoDatabase>();
            _collection = Substitute.For<IMongoCollection<PropertyTransactionCategory>>();

            // Configure database
            _database.GetCollection<PropertyTransactionCategory>(typeof(PropertyTransactionCategory).Name)
                .Returns(_collection);

            // Setup indexes
            var indexManager = Substitute.For<IMongoIndexManager<PropertyTransactionCategory>>();
            _collection.Indexes.Returns(indexManager);

            // Create repository
            _repository = new TransactionCategoryRepository(_database);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCategories_WhenCategoriesExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var expectedCategories = new List<PropertyTransactionCategory>
            {
                new PropertyTransactionCategory { TenantId = tenantId, Name = "Rent" },
                new PropertyTransactionCategory { TenantId = tenantId, Name = "Utilities" }
            };

            var cursor = Substitute.For<IAsyncCursor<PropertyTransactionCategory>>();
            cursor.Current.Returns(expectedCategories);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<FindOptions<PropertyTransactionCategory, PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEquivalentTo(expectedCategories);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoCategoriesExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var cursor = Substitute.For<IAsyncCursor<PropertyTransactionCategory>>();
            cursor.Current.Returns(new List<PropertyTransactionCategory>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<FindOptions<PropertyTransactionCategory, PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ShouldThrowMongoException_WhenDatabaseError()
        {
            // Arrange
            var tenantId = "tenant123";
            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<FindOptions<PropertyTransactionCategory, PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>())
                .ThrowsAsync(new MongoException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() => _repository.GetAllAsync(tenantId));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCategory_WhenCategoryExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var categoryId = ObjectId.GenerateNewId();
            var expectedCategory = new PropertyTransactionCategory { TenantId = tenantId, Name = "Rent" };
            expectedCategory.GetType().GetProperty("Id")?.SetValue(expectedCategory, categoryId);

            var cursor = Substitute.For<IAsyncCursor<PropertyTransactionCategory>>();
            cursor.Current.Returns(new List<PropertyTransactionCategory> { expectedCategory });
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<FindOptions<PropertyTransactionCategory, PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, categoryId.ToString());

            // Assert
            result.Should().BeEquivalentTo(expectedCategory);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var categoryId = ObjectId.GenerateNewId().ToString();

            var cursor = Substitute.For<IAsyncCursor<PropertyTransactionCategory>>();
            cursor.Current.Returns(new List<PropertyTransactionCategory>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<FindOptions<PropertyTransactionCategory, PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, categoryId);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid-id")]
        public async Task GetByIdAsync_ShouldThrowFormatException_WhenIdIsInvalid(string categoryId)
        {
            // Arrange
            var tenantId = "tenant123";

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() =>
                _repository.GetByIdAsync(tenantId, categoryId));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenTenantIdIsInvalid(string tenantId)
        {
            // Arrange
            var categoryId = ObjectId.GenerateNewId().ToString();
            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<FindOptions<PropertyTransactionCategory, PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>())
                .ThrowsAsync(new ArgumentException("Tenant ID cannot be null or empty"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _repository.GetByIdAsync(tenantId, categoryId));
            exception.Message.Should().Contain("Tenant ID cannot be null or empty");
        }

        [Fact]
        public async Task CreateAsync_ShouldInsertCategory_WhenValidCategoryProvided()
        {
            // Arrange
            var category = new PropertyTransactionCategory
            {
                TenantId = "tenant123",
                Name = "Utilities",
                Description = "Utility payments"
            };

            // Act
            await _repository.CreateAsync(category);

            // Assert
            await _collection.Received(1).InsertOneAsync(
                Arg.Is<PropertyTransactionCategory>(c => 
                    c.TenantId == category.TenantId && 
                    c.Name == category.Name),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentNullException_WhenCategoryIsNull()
        {
            // Act & Assert
#pragma warning disable CS8625
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.CreateAsync(null));
#pragma warning restore CS8625
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateCategory_WhenCategoryExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var categoryId = ObjectId.GenerateNewId().ToString();
            var category = new PropertyTransactionCategory
            {
                TenantId = tenantId,
                Name = "Updated Category"
            };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<PropertyTransactionCategory>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(1, 1, null));

            // Act
            await _repository.UpdateAsync(tenantId, categoryId, category);

            // Assert
            await _collection.Received(1).ReplaceOneAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<PropertyTransactionCategory>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteCategory_WhenCategoryExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var categoryId = ObjectId.GenerateNewId().ToString();

            _collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>())
                .Returns(new DeleteResult.Acknowledged(1));

            // Act
            await _repository.DeleteAsync(tenantId, categoryId);

            // Assert
            await _collection.Received(1).DeleteOneAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteAsync_ShouldThrowArgumentException_WhenTenantIdIsInvalid(string tenantId)
        {
            // Arrange
            var categoryId = ObjectId.GenerateNewId().ToString();
            _collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>())
                .ThrowsAsync(new ArgumentException("Tenant ID cannot be null or empty"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _repository.DeleteAsync(tenantId, categoryId));
            exception.Message.Should().Contain("Tenant ID cannot be null or empty");
        }
    }
}