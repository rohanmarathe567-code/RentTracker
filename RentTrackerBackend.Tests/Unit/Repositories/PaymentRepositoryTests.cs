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
    public class PaymentRepositoryTests
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<RentalPayment> _collection;
        private readonly IMongoCollection<PaymentMethod> _paymentMethodCollection;
        private readonly IOptions<MongoDbSettings> _settings;
        private readonly PaymentRepository _repository;
        private readonly string _databaseName = "testdb";

        public PaymentRepositoryTests()
        {
            // Create substitutes
            _mongoClient = Substitute.For<IMongoClient>();
            _database = Substitute.For<IMongoDatabase>();
            _collection = Substitute.For<IMongoCollection<RentalPayment>>();
            _paymentMethodCollection = Substitute.For<IMongoCollection<PaymentMethod>>();
            _settings = Substitute.For<IOptions<MongoDbSettings>>();

            // Configure settings
            _settings.Value.Returns(new MongoDbSettings { 
                DatabaseName = _databaseName,
                ConnectionString = "mongodb://localhost:27017"
            });

            // Configure database
            _mongoClient.GetDatabase(_databaseName).Returns(_database);
            _database.GetCollection<RentalPayment>(nameof(RentalPayment)).Returns(_collection);
            _database.GetCollection<PaymentMethod>(nameof(PaymentMethod)).Returns(_paymentMethodCollection);

            // Setup indexes
            var indexManager = Substitute.For<IMongoIndexManager<RentalPayment>>();
            _collection.Indexes.Returns(indexManager);

            // Create repository
            _repository = new PaymentRepository(_mongoClient, _settings);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllPayments_WhenPaymentsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var expectedPayments = new List<RentalPayment>
            {
                new RentalPayment { TenantId = tenantId },
                new RentalPayment { TenantId = tenantId }
            };

            var cursor = Substitute.For<IAsyncCursor<RentalPayment>>();
            cursor.Current.Returns(expectedPayments);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalPayment>>(),
                Arg.Any<FindOptions<RentalPayment, RentalPayment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEquivalentTo(expectedPayments);
        }

        [Fact]
        public async Task GetAllAsync_ShouldIncludePaymentMethods_WhenIncludesContainsPaymentMethod()
        {
            // Arrange
            var tenantId = "tenant123";
            var paymentMethodId = ObjectId.GenerateNewId().ToString();
            var expectedPayments = new List<RentalPayment>
            {
                new RentalPayment { TenantId = tenantId, PaymentMethodId = paymentMethodId }
            };

            var expectedPaymentMethod = new PaymentMethod { Id = ObjectId.Parse(paymentMethodId) };

            var paymentsCursor = Substitute.For<IAsyncCursor<RentalPayment>>();
            paymentsCursor.Current.Returns(expectedPayments);
            paymentsCursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            var paymentMethodsCursor = Substitute.For<IAsyncCursor<PaymentMethod>>();
            paymentMethodsCursor.Current.Returns(new List<PaymentMethod> { expectedPaymentMethod });
            paymentMethodsCursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalPayment>>(),
                Arg.Any<FindOptions<RentalPayment, RentalPayment>>(),
                Arg.Any<CancellationToken>())
                .Returns(paymentsCursor);

            _paymentMethodCollection.FindAsync(
                Arg.Any<FilterDefinition<PaymentMethod>>(),
                Arg.Any<FindOptions<PaymentMethod, PaymentMethod>>(),
                Arg.Any<CancellationToken>())
                .Returns(paymentMethodsCursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId, new[] { "PaymentMethod" });

            // Assert
            result.Should().ContainSingle()
                .Which.PaymentMethod.Should().BeEquivalentTo(expectedPaymentMethod);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnPayment_WhenPaymentExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var paymentId = ObjectId.GenerateNewId();
            var expectedPayment = new RentalPayment { TenantId = tenantId };
            expectedPayment.GetType().GetProperty("Id")?.SetValue(expectedPayment, paymentId);

            var cursor = Substitute.For<IAsyncCursor<RentalPayment>>();
            cursor.Current.Returns(new List<RentalPayment> { expectedPayment });
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalPayment>>(),
                Arg.Any<FindOptions<RentalPayment, RentalPayment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, paymentId.ToString());

            // Assert
            result.Should().BeEquivalentTo(expectedPayment);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenPaymentDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var paymentId = ObjectId.GenerateNewId().ToString();

            var cursor = Substitute.For<IAsyncCursor<RentalPayment>>();
            cursor.Current.Returns(new List<RentalPayment>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalPayment>>(),
                Arg.Any<FindOptions<RentalPayment, RentalPayment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, paymentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldInsertPayment_WhenValidPaymentProvided()
        {
            // Arrange
            var payment = new RentalPayment 
            { 
                TenantId = "tenant123",
                RentalPropertyId = ObjectId.GenerateNewId().ToString(),
                Amount = 1000.00m,
                PaymentDate = DateTime.UtcNow
            };

            // Act
            await _repository.CreateAsync(payment);

            // Assert
            await _collection.Received(1).InsertOneAsync(
                Arg.Is<RentalPayment>(p => p.TenantId == payment.TenantId),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentNullException_WhenPaymentIsNull()
        {
            // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.CreateAsync(null));
#pragma warning restore CS8625

            Assert.Contains("Value cannot be null. (Parameter 'entity')", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowInvalidOperationException_WhenPaymentNotFound()
        {
            // Arrange
            var tenantId = "tenant123";
            var paymentId = ObjectId.GenerateNewId();
            var payment = new RentalPayment { TenantId = tenantId };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<RentalPayment>>(),
                Arg.Any<RentalPayment>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(0, 0, null));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.UpdateAsync(tenantId, paymentId.ToString(), payment));
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeletePayment_WhenPaymentExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var paymentId = ObjectId.GenerateNewId().ToString();

            _collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<RentalPayment>>(),
                Arg.Any<CancellationToken>())
                .Returns(new DeleteResult.Acknowledged(1));

            // Act
            await _repository.DeleteAsync(tenantId, paymentId);

            // Assert
            await _collection.Received(1).DeleteOneAsync(
                Arg.Any<FilterDefinition<RentalPayment>>(),
                Arg.Any<CancellationToken>());
        }

       [Theory]
       [InlineData("")]
       [InlineData(" ")]
#pragma warning disable xUnit1012 // Null should not be used for parameter in Theory
       [InlineData(null)]
#pragma warning restore xUnit1012
       public async Task GetAllAsync_ShouldThrowArgumentException_WhenTenantIdIsInvalid(string? tenantId)
       {
           // Act & Assert
#pragma warning disable CS8604 // Possible null reference argument
           var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
               _repository.GetAllAsync(tenantId));
#pragma warning restore CS8604

           Assert.Contains("Tenant ID cannot be null or empty", exception.Message);
       }

       [Fact]
       public async Task GetByIdAsync_ShouldThrowArgumentException_WhenInvalidObjectId()
       {
           // Arrange
           var invalidId = "not-an-objectid";

           // Act & Assert
           var exception = await Assert.ThrowsAsync<FormatException>(() =>
               _repository.GetByIdAsync("tenant123", invalidId));

           Assert.Contains("Invalid ObjectId format", exception.Message);
       }

       [Theory]
       [InlineData(-1000.00)]
       [InlineData(0)]
       public async Task CreateAsync_ShouldThrowArgumentException_WhenInvalidAmount(decimal amount)
       {
           // Arrange
           var payment = new RentalPayment
           {
               TenantId = "tenant123",
               RentalPropertyId = ObjectId.GenerateNewId().ToString(),
               Amount = amount,
               PaymentDate = DateTime.UtcNow
           };

           // Act & Assert
           var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
               _repository.CreateAsync(payment));

           Assert.Contains("Payment amount must be greater than zero (Parameter 'entity')", exception.Message);
       }

       [Fact]
       public async Task UpdateAsync_ShouldThrowArgumentException_WhenInvalidId()
       {
           // Arrange
           var invalidId = "not-an-objectid";
           var payment = new RentalPayment { TenantId = "tenant123" };

           // Act & Assert
           var exception = await Assert.ThrowsAsync<FormatException>(() =>
               _repository.UpdateAsync("tenant123", invalidId, payment));

           Assert.Contains("Invalid ObjectId format", exception.Message);
       }

       [Fact]
       public async Task CreateAsync_ShouldThrowArgumentNullException_WhenEntityIsNull()
       {
           // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
           await Assert.ThrowsAsync<ArgumentNullException>(() =>
               _repository.CreateAsync(null));
#pragma warning restore CS8625
       }

       [Fact]
       public async Task GetAllAsync_ShouldHandleMissingPaymentMethod_WhenIncludesContainsPaymentMethod()
       {
           // Arrange
           var tenantId = "tenant123";
           var paymentMethodId = ObjectId.GenerateNewId().ToString();
           var expectedPayments = new List<RentalPayment>
           {
               new RentalPayment { TenantId = tenantId, PaymentMethodId = paymentMethodId }
           };

           var paymentsCursor = Substitute.For<IAsyncCursor<RentalPayment>>();
           paymentsCursor.Current.Returns(expectedPayments);
           paymentsCursor.MoveNextAsync(Arg.Any<CancellationToken>())
               .Returns(true, false);

           var emptyPaymentMethodsCursor = Substitute.For<IAsyncCursor<PaymentMethod>>();
           emptyPaymentMethodsCursor.Current.Returns(new List<PaymentMethod>());
           emptyPaymentMethodsCursor.MoveNextAsync(Arg.Any<CancellationToken>())
               .Returns(false);

           _collection.FindAsync(
               Arg.Any<FilterDefinition<RentalPayment>>(),
               Arg.Any<FindOptions<RentalPayment, RentalPayment>>(),
               Arg.Any<CancellationToken>())
               .Returns(paymentsCursor);

           _paymentMethodCollection.FindAsync(
               Arg.Any<FilterDefinition<PaymentMethod>>(),
               Arg.Any<FindOptions<PaymentMethod, PaymentMethod>>(),
               Arg.Any<CancellationToken>())
               .Returns(emptyPaymentMethodsCursor);

           // Act
           var result = await _repository.GetAllAsync(tenantId, new[] { "PaymentMethod" });

           // Assert
           result.Should().ContainSingle()
               .Which.PaymentMethod.Should().BeNull();
       }

       [Fact]
       public async Task GetByPropertyIdAsync_ShouldReturnPayment_WhenPaymentExists()
       {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();
            var expectedPayment = new RentalPayment 
            { 
                TenantId = tenantId,
                RentalPropertyId = propertyId
            };

            var cursor = Substitute.For<IAsyncCursor<RentalPayment>>();
            cursor.Current.Returns(new List<RentalPayment> { expectedPayment });
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalPayment>>(),
                Arg.Any<FindOptions<RentalPayment, RentalPayment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByPropertyIdAsync(tenantId, propertyId);

            // Assert
            result.Should().BeEquivalentTo(expectedPayment);
        }

        [Fact]
        public async Task GetByPropertyIdAsync_ShouldReturnNull_WhenPaymentDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();

            var cursor = Substitute.For<IAsyncCursor<RentalPayment>>();
            cursor.Current.Returns(new List<RentalPayment>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalPayment>>(),
                Arg.Any<FindOptions<RentalPayment, RentalPayment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByPropertyIdAsync(tenantId, propertyId);

            // Assert
            result.Should().BeNull();
        }
    }
}