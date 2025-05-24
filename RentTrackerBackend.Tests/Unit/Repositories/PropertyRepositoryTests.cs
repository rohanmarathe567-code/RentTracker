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
    public class PropertyRepositoryTests
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<RentalProperty> _collection;
        private readonly IOptions<MongoDbSettings> _settings;
        private readonly PropertyRepository _repository;
        private readonly string _databaseName = "testdb";

        public PropertyRepositoryTests()
        {
            // Create substitutes
            _mongoClient = Substitute.For<IMongoClient>();
            _database = Substitute.For<IMongoDatabase>();
            _collection = Substitute.For<IMongoCollection<RentalProperty>>();
            _settings = Substitute.For<IOptions<MongoDbSettings>>();

            // Configure settings
            _settings.Value.Returns(new MongoDbSettings { 
                DatabaseName = _databaseName,
                ConnectionString = "mongodb://localhost:27017"
            });

            // Configure database
            _mongoClient.GetDatabase(_databaseName).Returns(_database);
            _database.GetCollection<RentalProperty>(typeof(RentalProperty).Name).Returns(_collection);

            // Setup indexes
            var indexManager = Substitute.For<IMongoIndexManager<RentalProperty>>();
            _collection.Indexes.Returns(indexManager);

            // Create repository
            _repository = new PropertyRepository(_mongoClient, _settings);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllProperties_WhenPropertiesExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var expectedProperties = new List<RentalProperty>
            {
                new RentalProperty { TenantId = tenantId },
                new RentalProperty { TenantId = tenantId }
            };

            var cursor = Substitute.For<IAsyncCursor<RentalProperty>>();
            cursor.Current.Returns(expectedProperties);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<FindOptions<RentalProperty, RentalProperty>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEquivalentTo(expectedProperties);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoPropertiesExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var cursor = Substitute.For<IAsyncCursor<RentalProperty>>();
            cursor.Current.Returns(new List<RentalProperty>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<FindOptions<RentalProperty, RentalProperty>>(),
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
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<FindOptions<RentalProperty, RentalProperty>>(),
                Arg.Any<CancellationToken>())
                .ThrowsAsync(new MongoException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() => _repository.GetAllAsync(tenantId));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnProperty_WhenPropertyExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId();
            var expectedProperty = new RentalProperty { TenantId = tenantId };
            expectedProperty.GetType().GetProperty("Id")?.SetValue(expectedProperty, propertyId);

            var cursor = Substitute.For<IAsyncCursor<RentalProperty>>();
            cursor.Current.Returns(new List<RentalProperty> { expectedProperty });
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<FindOptions<RentalProperty, RentalProperty>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, propertyId.ToString());

            // Assert
            result.Should().BeEquivalentTo(expectedProperty);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenPropertyDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();

            var cursor = Substitute.For<IAsyncCursor<RentalProperty>>();
            cursor.Current.Returns(new List<RentalProperty>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<FindOptions<RentalProperty, RentalProperty>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, propertyId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldInsertProperty_WhenValidPropertyProvided()
        {
            // Arrange
            var property = new RentalProperty 
            { 
                TenantId = "tenant123",
                Address = new Address { 
                    Street = "123 Test St", 
                    City = "TestCity", 
                    State = "TS", 
                    ZipCode = "12345" 
                }
            };

            // Act
            await _repository.CreateAsync(property);

            // Assert
            await _collection.Received(1).InsertOneAsync(
                Arg.Is<RentalProperty>(p => p.TenantId == property.TenantId),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentNullException_WhenPropertyIsNull()
        {
            // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.CreateAsync(null));
#pragma warning restore CS8625
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowInvalidOperationException_WhenPropertyNotFound()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId();
            var property = new RentalProperty { TenantId = tenantId };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<RentalProperty>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(0, 0, null));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.UpdateAsync(tenantId, propertyId.ToString(), property));
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteProperty_WhenPropertyExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = ObjectId.GenerateNewId().ToString();

            _collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<CancellationToken>())
                .Returns(new DeleteResult.Acknowledged(1));

            // Act
            await _repository.DeleteAsync(tenantId, propertyId);

            // Assert
            await _collection.Received(1).DeleteOneAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetPropertiesByRentRangeAsync_ShouldReturnProperties_WhenPropertiesExistInRange()
        {
            // Arrange
            var tenantId = "tenant123";
            decimal minRent = 1000;
            decimal maxRent = 2000;
            var expectedProperties = new List<RentalProperty>
            {
                new RentalProperty { TenantId = tenantId, RentAmount = 1500 },
                new RentalProperty { TenantId = tenantId, RentAmount = 1800 }
            };

            var cursor = Substitute.For<IAsyncCursor<RentalProperty>>();
            cursor.Current.Returns(expectedProperties);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<FindOptions<RentalProperty, RentalProperty>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetPropertiesByRentRangeAsync(tenantId, minRent, maxRent);

            // Assert
            result.Should().BeEquivalentTo(expectedProperties);
        }

        [Fact]
        public async Task GetPropertiesByCityAsync_ShouldReturnProperties_WhenPropertiesExistInCity()
        {
            // Arrange
            var tenantId = "tenant123";
            var city = "TestCity";
            var expectedProperties = new List<RentalProperty>
            {
                new RentalProperty {
                    TenantId = tenantId,
                    Address = new Address { City = city }
                },
                new RentalProperty {
                    TenantId = tenantId,
                    Address = new Address { City = city }
                }
            };

            var cursor = Substitute.For<IAsyncCursor<RentalProperty>>();
            cursor.Current.Returns(expectedProperties);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<FindOptions<RentalProperty, RentalProperty>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetPropertiesByCityAsync(tenantId, city);

            // Assert
            result.Should().BeEquivalentTo(expectedProperties);
        }

        [Fact]
        public async Task GetPropertiesByCityAsync_ShouldReturnEmptyList_WhenNoPropertiesExistInCity()
        {
            // Arrange
            var tenantId = "tenant123";
            var city = "NonExistentCity";

            var cursor = Substitute.For<IAsyncCursor<RentalProperty>>();
            cursor.Current.Returns(new List<RentalProperty>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<FindOptions<RentalProperty, RentalProperty>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetPropertiesByCityAsync(tenantId, city);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("", "TestCity")]
        [InlineData("   ", "TestCity")]
        public async Task GetPropertiesByCityAsync_ShouldThrowArgumentException_WhenTenantIdIsInvalid(string tenantId, string city)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _repository.GetPropertiesByCityAsync(tenantId, city));
        }

        [Theory]
        [InlineData("tenant123", "")]
        [InlineData("tenant123", "   ")]
        public async Task GetPropertiesByCityAsync_ShouldThrowArgumentException_WhenCityIsInvalid(string tenantId, string city)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _repository.GetPropertiesByCityAsync(tenantId, city));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData("test", true)]
        [InlineData("123 Main St", true)]
        public async Task SearchPropertiesAsync_ShouldHandleEmptyAndValidSearchText(string searchText, bool shouldHaveResults)
        {
            // Arrange
            var tenantId = "tenant123";
            var properties = shouldHaveResults 
                ? new List<RentalProperty> { new RentalProperty { TenantId = tenantId } }
                : new List<RentalProperty>();

            var cursor = Substitute.For<IAsyncCursor<RentalProperty>>();
            cursor.Current.Returns(properties);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<RentalProperty>>(),
                Arg.Any<FindOptions<RentalProperty, RentalProperty>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.SearchPropertiesAsync(tenantId, searchText);

            // Assert
            if (shouldHaveResults)
                result.Should().NotBeEmpty();
            else
                result.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowArgumentNullException_WhenSearchTextIsNull()
        {
            // Arrange
            var tenantId = "tenant123";
#pragma warning disable CS8600, CS8604 // Converting null literal or possible null value to non-nullable type
            string searchText = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.SearchPropertiesAsync(tenantId, searchText));
#pragma warning restore CS8600, CS8604
        }
    }
}