using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Tests.Unit.Repositories
{
    public class PaymentMethodRepositoryTests
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<PaymentMethod> _collection;
        private readonly IOptions<MongoDbSettings> _settings;
        private readonly PaymentMethodRepository _repository;
        private readonly string _databaseName = "testdb";

        public PaymentMethodRepositoryTests()
        {
            // Create substitutes
            _mongoClient = Substitute.For<IMongoClient>();
            _database = Substitute.For<IMongoDatabase>();
            _collection = Substitute.For<IMongoCollection<PaymentMethod>>();
            _settings = Substitute.For<IOptions<MongoDbSettings>>();

            // Configure settings
            _settings.Value.Returns(new MongoDbSettings { 
                DatabaseName = _databaseName,
                ConnectionString = "mongodb://localhost:27017"
            });

            // Configure database
            _mongoClient.GetDatabase(_databaseName).Returns(_database);
            _database.GetCollection<PaymentMethod>(typeof(PaymentMethod).Name).Returns(_collection);

            // Setup indexes
            var indexManager = Substitute.For<IMongoIndexManager<PaymentMethod>>();
            _collection.Indexes.Returns(indexManager);

            // Create repository
            _repository = new PaymentMethodRepository(_database);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllPaymentMethods_WhenPaymentMethodsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var expectedPaymentMethods = new List<PaymentMethod>
            {
                new PaymentMethod { TenantId = tenantId, Name = "Credit Card" },
                new PaymentMethod { TenantId = tenantId, Name = "Bank Transfer" }
            };

            var cursor = Substitute.For<IAsyncCursor<PaymentMethod>>();
            cursor.Current.Returns(expectedPaymentMethods);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<FindOptions<PaymentMethod, PaymentMethod>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEquivalentTo(expectedPaymentMethods);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoPaymentMethodsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var cursor = Substitute.For<IAsyncCursor<PaymentMethod>>();
            cursor.Current.Returns(new List<PaymentMethod>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<FindOptions<PaymentMethod, PaymentMethod>>(),
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
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<FindOptions<PaymentMethod, PaymentMethod>>(),
                Arg.Any<CancellationToken>())
                .ThrowsAsync(new MongoException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() => _repository.GetAllAsync(tenantId));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnPaymentMethod_WhenPaymentMethodExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var paymentMethodId = ObjectId.GenerateNewId();
            var expectedPaymentMethod = new PaymentMethod { TenantId = tenantId, Name = "Credit Card" };
            expectedPaymentMethod.GetType().GetProperty("Id")?.SetValue(expectedPaymentMethod, paymentMethodId);

            var cursor = Substitute.For<IAsyncCursor<PaymentMethod>>();
            cursor.Current.Returns(new List<PaymentMethod> { expectedPaymentMethod });
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<FindOptions<PaymentMethod, PaymentMethod>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, paymentMethodId.ToString());

            // Assert
            result.Should().BeEquivalentTo(expectedPaymentMethod);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenPaymentMethodDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var paymentMethodId = ObjectId.GenerateNewId().ToString();

            var cursor = Substitute.For<IAsyncCursor<PaymentMethod>>();
            cursor.Current.Returns(new List<PaymentMethod>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<FindOptions<PaymentMethod, PaymentMethod>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, paymentMethodId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldInsertPaymentMethod_WhenValidPaymentMethodProvided()
        {
            // Arrange
            var paymentMethod = new PaymentMethod
            {
                TenantId = "tenant123",
                Name = "Credit Card",
                Description = "Primary credit card"
            };

            // Act
            await _repository.CreateAsync(paymentMethod);

            // Assert
            await _collection.Received(1).InsertOneAsync(
                Arg.Is<PaymentMethod>(p => p.TenantId == paymentMethod.TenantId && p.Name == paymentMethod.Name),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentNullException_WhenPaymentMethodIsNull()
        {
            // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.CreateAsync(null));
#pragma warning restore CS8625
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowInvalidOperationException_WhenPaymentMethodNotFound()
        {
            // Arrange
            var tenantId = "tenant123";
            var paymentMethodId = ObjectId.GenerateNewId();
            var paymentMethod = new PaymentMethod { TenantId = tenantId };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<PaymentMethod>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(0, 0, null));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _repository.UpdateAsync(tenantId, paymentMethodId.ToString(), paymentMethod));
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeletePaymentMethod_WhenPaymentMethodExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var paymentMethodId = ObjectId.GenerateNewId().ToString();

            _collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<CancellationToken>())
                .Returns(new DeleteResult.Acknowledged(1));

            // Act
            await _repository.DeleteAsync(tenantId, paymentMethodId);

            // Assert
            await _collection.Received(1).DeleteOneAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenTenantIdIsEmpty(string tenantId)
        {
            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByIdAsync_ShouldThrowFormatException_WhenPaymentMethodIdIsInvalid(string paymentMethodId)
        {
            // Arrange
            var tenantId = "tenant123";

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => _repository.GetByIdAsync(tenantId, paymentMethodId));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowFormatException_WhenPaymentMethodIdIsNull()
        {
            // Arrange
            var tenantId = "tenant123";

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => _repository.GetByIdAsync(tenantId, null!));
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenTenantIdIsNull()
        {
            // Act
            var result = await _repository.GetAllAsync(null!);

            // Assert
            result.Should().BeEmpty();
        }
    }
}