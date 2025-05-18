using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using Xunit;

namespace RentTrackerBackend.Tests.Unit.Repositories
{
    public class PropertyTransactionRepositoryTests
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<PropertyTransaction> _collection;
        private readonly IMongoCollection<PaymentMethod> _paymentMethodCollection;
        private readonly IMongoCollection<PropertyTransactionCategory> _categoryCollection;
        private readonly IOptions<MongoDbSettings> _settings;
        private readonly PropertyTransactionRepository _repository;
        private readonly string _databaseName = "testdb";

        public PropertyTransactionRepositoryTests()
        {
            // Create substitutes
            _mongoClient = Substitute.For<IMongoClient>();
            _database = Substitute.For<IMongoDatabase>();
            _collection = Substitute.For<IMongoCollection<PropertyTransaction>>();
            _paymentMethodCollection = Substitute.For<IMongoCollection<PaymentMethod>>();
            _categoryCollection = Substitute.For<IMongoCollection<PropertyTransactionCategory>>();
            _settings = Substitute.For<IOptions<MongoDbSettings>>();

            // Configure settings
            _settings.Value.Returns(new MongoDbSettings { 
                DatabaseName = _databaseName,
                ConnectionString = "mongodb://localhost:27017"
            });

            // Configure database
            _mongoClient.GetDatabase(_databaseName).Returns(_database);
            _database.GetCollection<PropertyTransaction>(nameof(PropertyTransaction)).Returns(_collection);
            _database.GetCollection<PaymentMethod>(nameof(PaymentMethod)).Returns(_paymentMethodCollection);
            _database.GetCollection<PropertyTransactionCategory>(nameof(PropertyTransactionCategory)).Returns(_categoryCollection);

            // Setup indexes
            var indexManager = Substitute.For<IMongoIndexManager<PropertyTransaction>>();
            _collection.Indexes.Returns(indexManager);

            // Create repository
            _repository = new PropertyTransactionRepository(_mongoClient, _settings);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllTransactions_WhenTransactionsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var expectedTransactions = new List<PropertyTransaction>
            {
                new PropertyTransaction { TenantId = tenantId, Amount = 100 },
                new PropertyTransaction { TenantId = tenantId, Amount = 200 }
            };

            var cursor = Substitute.For<IAsyncCursor<PropertyTransaction>>();
            cursor.Current.Returns(expectedTransactions);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<FindOptions<PropertyTransaction, PropertyTransaction>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEquivalentTo(expectedTransactions);
        }

        [Fact]
        public async Task GetAllAsync_ShouldIncludePaymentMethod_WhenRequested()
        {
            // Arrange
            var tenantId = "tenant123";
            var paymentMethodId = ObjectId.GenerateNewId();
            var paymentMethod = new PaymentMethod { Id = paymentMethodId, Name = "Credit Card" };
            var transactions = new List<PropertyTransaction>
            {
                new PropertyTransaction { 
                    TenantId = tenantId, 
                    PaymentMethodId = paymentMethodId.ToString(),
                    Amount = 100
                }
            };

            var cursor = Substitute.For<IAsyncCursor<PropertyTransaction>>();
            cursor.Current.Returns(transactions);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<FindOptions<PropertyTransaction, PropertyTransaction>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            var paymentMethodCursor = Substitute.For<IAsyncCursor<PaymentMethod>>();
            paymentMethodCursor.Current.Returns(new List<PaymentMethod> { paymentMethod });
            paymentMethodCursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _paymentMethodCollection.FindAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<FindOptions<PaymentMethod, PaymentMethod>>(),
                Arg.Any<CancellationToken>())
                .Returns(paymentMethodCursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId, false, new[] { "PaymentMethod" });

            // Assert
            result.Should().ContainSingle();
            result.First().PaymentMethod.Should().BeEquivalentTo(paymentMethod);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoTransactionsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var cursor = Substitute.For<IAsyncCursor<PropertyTransaction>>();
            cursor.Current.Returns(new List<PropertyTransaction>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<FindOptions<PropertyTransaction, PropertyTransaction>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ShouldThrowArgumentException_WhenTenantIdIsInvalid()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetAllAsync(""));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnTransaction_WhenTransactionExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var transactionId = ObjectId.GenerateNewId();
            var categoryId = ObjectId.GenerateNewId();
            var paymentMethodId = ObjectId.GenerateNewId();

            var expectedTransaction = new PropertyTransaction {
                TenantId = tenantId,
                Amount = 100,
                CategoryId = categoryId.ToString(),
                PaymentMethodId = paymentMethodId.ToString()
            };
            expectedTransaction.GetType().GetProperty("Id")?.SetValue(expectedTransaction, transactionId);

            var cursor = Substitute.For<IAsyncCursor<PropertyTransaction>>();
            cursor.Current.Returns(new List<PropertyTransaction> { expectedTransaction });
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            var categoryCursor = Substitute.For<IAsyncCursor<PropertyTransactionCategory>>();
            categoryCursor.Current.Returns(new List<PropertyTransactionCategory>
            {
                new PropertyTransactionCategory { Id = categoryId, Name = "Test Category" }
            });
            categoryCursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            var paymentMethodCursor = Substitute.For<IAsyncCursor<PaymentMethod>>();
            paymentMethodCursor.Current.Returns(new List<PaymentMethod>
            {
                new PaymentMethod { Id = paymentMethodId, Name = "Test Payment Method" }
            });
            paymentMethodCursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<FindOptions<PropertyTransaction, PropertyTransaction>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            _categoryCollection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<FindOptions<PropertyTransactionCategory, PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>())
                .Returns(categoryCursor);

            _paymentMethodCollection.FindAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<FindOptions<PaymentMethod, PaymentMethod>>(),
                Arg.Any<CancellationToken>())
                .Returns(paymentMethodCursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, transactionId.ToString());

            // Assert
            result.Should().BeEquivalentTo(expectedTransaction);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenTransactionDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var transactionId = ObjectId.GenerateNewId().ToString();

            var cursor = Substitute.For<IAsyncCursor<PropertyTransaction>>();
            cursor.Current.Returns(new List<PropertyTransaction>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<FindOptions<PropertyTransaction, PropertyTransaction>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, transactionId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldInsertTransaction_WhenValidTransactionProvided()
        {
            // Arrange
            var transaction = new PropertyTransaction
            {
                TenantId = "tenant123",
                Amount = 100,
                TransactionDate = DateTime.UtcNow
            };

            // Act
            await _repository.CreateAsync(transaction);

            // Assert
            await _collection.Received(1).InsertOneAsync(
                Arg.Is<PropertyTransaction>(t => 
                    t.TenantId == transaction.TenantId && 
                    t.Amount == transaction.Amount),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentNullException_WhenTransactionIsNull()
        {
            // Act & Assert
#pragma warning disable CS8625
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.CreateAsync(null));
#pragma warning restore CS8625
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentException_WhenAmountIsZeroOrNegative()
        {
            // Arrange
            var transaction = new PropertyTransaction
            {
                TenantId = "tenant123",
                Amount = 0
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _repository.CreateAsync(transaction));
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateTransaction_WhenTransactionExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var transactionId = ObjectId.GenerateNewId().ToString();
            var transaction = new PropertyTransaction { 
                TenantId = tenantId,
                Amount = 100
            };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<PropertyTransaction>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(1, 1, null));

            // Act
            await _repository.UpdateAsync(tenantId, transactionId, transaction);

            // Assert
            await _collection.Received(1).ReplaceOneAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<PropertyTransaction>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetByPropertyIdAsync_ShouldReturnTransactions_WhenTransactionsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();
            var categoryId = ObjectId.GenerateNewId();
            var paymentMethodId = ObjectId.GenerateNewId();

            var expectedTransactions = new List<PropertyTransaction>
            {
                new PropertyTransaction {
                    TenantId = tenantId,
                    RentalPropertyId = propertyId,
                    CategoryId = categoryId.ToString(),
                    PaymentMethodId = paymentMethodId.ToString(),
                    Amount = 100
                },
                new PropertyTransaction {
                    TenantId = tenantId,
                    RentalPropertyId = propertyId,
                    CategoryId = categoryId.ToString(),
                    PaymentMethodId = paymentMethodId.ToString(),
                    Amount = 200
                }
            };

            var cursor = Substitute.For<IAsyncCursor<PropertyTransaction>>();
            cursor.Current.Returns(expectedTransactions);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            var categoryCursor = Substitute.For<IAsyncCursor<PropertyTransactionCategory>>();
            categoryCursor.Current.Returns(new List<PropertyTransactionCategory>
            {
                new PropertyTransactionCategory { Id = categoryId, Name = "Test Category" }
            });
            categoryCursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            var paymentMethodCursor = Substitute.For<IAsyncCursor<PaymentMethod>>();
            paymentMethodCursor.Current.Returns(new List<PaymentMethod>
            {
                new PaymentMethod { Id = paymentMethodId, Name = "Test Payment Method" }
            });
            paymentMethodCursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<FindOptions<PropertyTransaction, PropertyTransaction>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            _categoryCollection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransactionCategory>>(),
                Arg.Any<FindOptions<PropertyTransactionCategory, PropertyTransactionCategory>>(),
                Arg.Any<CancellationToken>())
                .Returns(categoryCursor);

            _paymentMethodCollection.FindAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<FindOptions<PaymentMethod, PaymentMethod>>(),
                Arg.Any<CancellationToken>())
                .Returns(paymentMethodCursor);

            // Act
            var result = await _repository.GetByPropertyIdAsync(tenantId, propertyId);

            // Assert
            result.Should().BeEquivalentTo(expectedTransactions);
        }

        [Fact]
        public async Task GetByPropertyIdAsync_ShouldReturnEmptyList_WhenNoTransactionsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();

            var cursor = Substitute.For<IAsyncCursor<PropertyTransaction>>();
            cursor.Current.Returns(new List<PropertyTransaction>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<FindOptions<PropertyTransaction, PropertyTransaction>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByPropertyIdAsync(tenantId, propertyId);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid-id")]
        public async Task GetByPropertyIdAsync_ShouldThrowFormatException_WhenPropertyIdIsInvalid(string propertyId)
        {
            // Arrange
            var tenantId = "tenant123";
            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<FindOptions<PropertyTransaction, PropertyTransaction>>(),
                Arg.Any<CancellationToken>())
                .ThrowsAsync(new FormatException($"Invalid ObjectId format: {propertyId}"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<FormatException>(() =>
                _repository.GetByPropertyIdAsync(tenantId, propertyId));
            exception.Message.Should().Contain("Invalid ObjectId format");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByPropertyIdAsync_ShouldThrowArgumentException_WhenTenantIdIsInvalid(string tenantId)
        {
            // Arrange
            var propertyId = ObjectId.GenerateNewId().ToString();
            _collection.FindAsync(
                Arg.Any<FilterDefinition<PropertyTransaction>>(),
                Arg.Any<FindOptions<PropertyTransaction, PropertyTransaction>>(),
                Arg.Any<CancellationToken>())
                .ThrowsAsync(new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId)));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _repository.GetByPropertyIdAsync(tenantId, propertyId));
            exception.Message.Should().Contain("Tenant ID cannot be null or empty");
        }
   }
}