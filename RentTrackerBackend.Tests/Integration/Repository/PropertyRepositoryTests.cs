using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Tests.Integration.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Mongo2Go;

namespace RentTrackerBackend.Tests.Integration.Repository
{
    [Collection("MongoDB")]
    public class PropertyRepositoryTests : IAsyncLifetime
    {
        private readonly string _tenantId = "test-tenant";
        private PropertyRepository _repository = null!;
        private MongoDbFixture _fixture = null!;

        public async Task InitializeAsync()
        {
            // Get or create the shared MongoDB instance
            _fixture = await MongoDbCollection.GetInstanceAsync();

            // Drop and recreate the database to ensure a clean state
            await _fixture.MongoClient.DropDatabaseAsync(_fixture.DatabaseName);
            await _fixture.CreateIndexesAsync();
        
            // Create the repository instance
            _repository = new PropertyRepository(_fixture.MongoClient, _fixture.Settings);
        }

        public async Task DisposeAsync()
        {
            // Drop the database after each test to ensure a clean state
            if (_fixture != null)
            {
                await _fixture.MongoClient.DropDatabaseAsync(_fixture.DatabaseName);
            }
        }


        private RentalProperty CreateTestProperty(string? city = null, decimal? rentAmount = null)
        {
            return new RentalProperty
            {
                TenantId = _tenantId,
                Address = new Address
                {
                    Street = "123 Test Street",
                    City = city ?? "Test City",
                    State = "TS",
                    ZipCode = "12345"
                },
                Description = "Test Property Description",
                PropertyManager = new PropertyManager
                {
                    Name = "Test Manager",
                    Contact = "manager@test.com"
                },
                RentAmount = rentAmount ?? 1000m,
                LeaseDates = new LeaseDates
                {
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow.AddMonths(11)
                }
            };
        }

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllProperties_WhenPropertiesExist()
        {
            // Arrange
            var property1 = await _repository.CreateAsync(CreateTestProperty());
            var property2 = await _repository.CreateAsync(CreateTestProperty());
            
            // Act
            var result = await _repository.GetAllAsync(_tenantId);
            
            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.Id == property1.Id);
            result.Should().Contain(p => p.Id == property2.Id);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoPropertiesExist()
        {
            // Act
            var result = await _repository.GetAllAsync(_tenantId);
            
            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetAllAsync_ShouldThrowArgumentException_WhenTenantIdIsEmptyOrWhitespace(string emptyTenantId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetAllAsync(emptyTenantId));
        }
        
        [Fact]
        public async Task GetAllAsync_ShouldThrowArgumentException_WhenTenantIdIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetAllAsync(null!));
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldReturnProperty_WhenPropertyExists()
        {
            // Arrange
            var property = await _repository.CreateAsync(CreateTestProperty());
            
            // Act
            var result = await _repository.GetByIdAsync(_tenantId, property.Id.ToString());
            
            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(property.Id);
            result.Address.City.Should().Be("Test City");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenPropertyDoesNotExist()
        {
            // Arrange
            var nonExistentId = ObjectId.GenerateNewId().ToString();
            
            // Act
            var result = await _repository.GetByIdAsync(_tenantId, nonExistentId);
            
            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowFormatException_WhenInvalidIdFormat()
        {
            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => 
                _repository.GetByIdAsync(_tenantId, "not-an-objectid"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenTenantIdIsEmptyOrWhitespace(string emptyTenantId)
        {
            // Arrange
            var id = ObjectId.GenerateNewId().ToString();
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetByIdAsync(emptyTenantId, id));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenTenantIdIsNull()
        {
            // Arrange
            var id = ObjectId.GenerateNewId().ToString();
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetByIdAsync(null!, id));
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldInsertProperty_WhenValidPropertyProvided()
        {
            // Arrange
            var property = CreateTestProperty();
            
            // Act
            var result = await _repository.CreateAsync(property);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBe(ObjectId.Empty);
            
            // Verify it was actually inserted in the database
            var retrievedProperty = await _repository.GetByIdAsync(_tenantId, result.Id.ToString());
            retrievedProperty.Should().NotBeNull();
            retrievedProperty!.Address.Street.Should().Be("123 Test Street");
        }

        [Fact]
        public async Task CreateAsync_ShouldSetMetadataFields_WhenPropertyCreated()
        {
            // Arrange
            var property = CreateTestProperty();
            var beforeCreate = DateTime.UtcNow;
            
            // Act
            var result = await _repository.CreateAsync(property);
            
            // Assert
            result.CreatedAt.Should().BeOnOrAfter(beforeCreate);
            result.UpdatedAt.Should().BeOnOrAfter(beforeCreate);
            result.Version.Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentNullException_WhenPropertyIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _repository.CreateAsync(null!));
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldUpdateProperty_WhenPropertyExists()
        {
            // Arrange
            var property = await _repository.CreateAsync(CreateTestProperty());
            
            // Update property details
            property.Description = "Updated Description";
            property.Address.City = "New City";
            property.RentAmount = 1200m;
            
            // Act
            await _repository.UpdateAsync(_tenantId, property.Id.ToString(), property);
            
            // Assert
            var updatedProperty = await _repository.GetByIdAsync(_tenantId, property.Id.ToString());
            updatedProperty.Should().NotBeNull();
            updatedProperty!.Description.Should().Be("Updated Description");
            updatedProperty.Address.City.Should().Be("New City");
            updatedProperty.RentAmount.Should().Be(1200m);
        }

        [Fact]
        public async Task UpdateAsync_ShouldIncrementVersion_WhenPropertyUpdated()
        {
            // Arrange
            var property = await _repository.CreateAsync(CreateTestProperty());
            var originalVersion = property.Version;
            
            // Update property
            property.Description = "Updated Description";
            
            // Act
            await _repository.UpdateAsync(_tenantId, property.Id.ToString(), property);
            
            // Assert
            var updatedProperty = await _repository.GetByIdAsync(_tenantId, property.Id.ToString());
            updatedProperty.Should().NotBeNull();
            updatedProperty!.Version.Should().Be(originalVersion + 1);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowInvalidOperationException_WhenPropertyNotFound()
        {
            // Arrange
            var property = CreateTestProperty();
            var nonExistentId = ObjectId.GenerateNewId().ToString();
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.UpdateAsync(_tenantId, nonExistentId, property));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowFormatException_WhenInvalidIdFormat()
        {
            // Arrange
            var property = CreateTestProperty();
            
            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => 
                _repository.UpdateAsync(_tenantId, "not-an-objectid", property));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task UpdateAsync_ShouldThrowArgumentException_WhenTenantIdIsEmptyOrWhitespace(string emptyTenantId)
        {
            // Arrange
            var property = CreateTestProperty();
            var id = ObjectId.GenerateNewId().ToString();
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.UpdateAsync(emptyTenantId, id, property));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowArgumentException_WhenTenantIdIsNull()
        {
            // Arrange
            var property = CreateTestProperty();
            var id = ObjectId.GenerateNewId().ToString();
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.UpdateAsync(null!, id, property));
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldDeleteProperty_WhenPropertyExists()
        {
            // Arrange
            var property = await _repository.CreateAsync(CreateTestProperty());
            
            // Act
            await _repository.DeleteAsync(_tenantId, property.Id.ToString());
            
            // Assert
            var result = await _repository.GetByIdAsync(_tenantId, property.Id.ToString());
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldNotThrowException_WhenPropertyDoesNotExist()
        {
            // Arrange
            var nonExistentId = ObjectId.GenerateNewId().ToString();
            
            // Act & Assert - Should not throw
            await _repository.DeleteAsync(_tenantId, nonExistentId);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowFormatException_WhenInvalidIdFormat()
        {
            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => 
                _repository.DeleteAsync(_tenantId, "not-an-objectid"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task DeleteAsync_ShouldThrowArgumentException_WhenTenantIdIsEmptyOrWhitespace(string emptyTenantId)
        {
            // Arrange
            var id = ObjectId.GenerateNewId().ToString();
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.DeleteAsync(emptyTenantId, id));
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowArgumentException_WhenTenantIdIsNull()
        {
            // Arrange
            var id = ObjectId.GenerateNewId().ToString();
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.DeleteAsync(null!, id));
        }

        #endregion

        #region GetPropertiesByCityAsync Tests

        [Fact]
        public async Task GetPropertiesByCityAsync_ShouldReturnProperties_WhenCityMatches()
        {
            // Arrange
            await _repository.CreateAsync(CreateTestProperty(city: "New York"));
            await _repository.CreateAsync(CreateTestProperty(city: "New York"));
            await _repository.CreateAsync(CreateTestProperty(city: "Boston"));
            
            // Act
            var result = await _repository.GetPropertiesByCityAsync(_tenantId, "New York");
            
            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(p => p.Address.City.Should().Be("New York"));
        }

        [Fact]
        public async Task GetPropertiesByCityAsync_ShouldReturnEmptyList_WhenNoCityMatches()
        {
            // Arrange
            await _repository.CreateAsync(CreateTestProperty(city: "New York"));
            await _repository.CreateAsync(CreateTestProperty(city: "Boston"));
            
            // Act
            var result = await _repository.GetPropertiesByCityAsync(_tenantId, "Chicago");
            
            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetPropertiesByCityAsync_ShouldThrowArgumentException_WhenCityIsEmptyOrWhitespace(string emptyCity)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetPropertiesByCityAsync(_tenantId, emptyCity));
        }

        [Fact]
        public async Task GetPropertiesByCityAsync_ShouldThrowArgumentException_WhenCityIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetPropertiesByCityAsync(_tenantId, null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetPropertiesByCityAsync_ShouldThrowArgumentException_WhenTenantIdIsEmptyOrWhitespace(string emptyTenantId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetPropertiesByCityAsync(emptyTenantId, "New York"));
        }

        [Fact]
        public async Task GetPropertiesByCityAsync_ShouldThrowArgumentException_WhenTenantIdIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetPropertiesByCityAsync(null!, "New York"));
        }

        #endregion

        #region GetPropertiesByRentRangeAsync Tests

        [Fact]
        public async Task GetPropertiesByRentRangeAsync_ShouldReturnProperties_WhenRentInRange()
        {
            // Arrange
            await _repository.CreateAsync(CreateTestProperty(rentAmount: 800m));
            await _repository.CreateAsync(CreateTestProperty(rentAmount: 1000m));
            await _repository.CreateAsync(CreateTestProperty(rentAmount: 1200m));
            await _repository.CreateAsync(CreateTestProperty(rentAmount: 1500m));
            
            // Verify all properties were created
            var allProperties = await _repository.GetAllAsync(_tenantId);
            Assert.Equal(4, allProperties.Count());

            // Act
            var result = await _repository.GetPropertiesByRentRangeAsync(_tenantId, 900m, 1300m);
            
            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(p => p.RentAmount.Should().BeInRange(900m, 1300m));
        }

        [Fact]
        public async Task GetPropertiesByRentRangeAsync_ShouldReturnEmptyList_WhenNoRentsInRange()
        {
            // Arrange
            await _repository.CreateAsync(CreateTestProperty(rentAmount: 800m));
            await _repository.CreateAsync(CreateTestProperty(rentAmount: 900m));
            
            // Act
            var result = await _repository.GetPropertiesByRentRangeAsync(_tenantId, 1000m, 1500m);
            
            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetPropertiesByRentRangeAsync_ShouldThrowArgumentException_WhenTenantIdIsEmptyOrWhitespace(string emptyTenantId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetPropertiesByRentRangeAsync(emptyTenantId, 1000m, 2000m));
        }

        [Fact]
        public async Task GetPropertiesByRentRangeAsync_ShouldThrowArgumentException_WhenTenantIdIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetPropertiesByRentRangeAsync(null!, 1000m, 2000m));
        }

        [Theory]
        [InlineData(-1000, 1000)]
        [InlineData(2000, 1000)]  // Min > Max
        public async Task GetPropertiesByRentRangeAsync_ShouldThrowArgumentException_WhenRentRangeIsInvalid(decimal min, decimal max)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetPropertiesByRentRangeAsync(_tenantId, min, max));
        }

        #endregion

        #region SearchPropertiesAsync Tests

        [Fact]
        public async Task SearchPropertiesAsync_ShouldReturnProperties_WhenTextMatches()
        {
            // Arrange - Reset collection and create test properties
            var property1 = CreateTestProperty();
            property1.Address.Street = "123 Maple Avenue";
            property1.Description = "Beautiful apartment with maple floors";
            await _repository.CreateAsync(property1);

            var property2 = CreateTestProperty();
            property2.Address.Street = "456 Oak Street";
            property2.Description = "Modern home with oak trees";
            await _repository.CreateAsync(property2);
            
            // Act - Search for properties containing "maple"
            var result = await _repository.SearchPropertiesAsync(_tenantId, "maple");
            
            // Assert
            result.Should().NotBeEmpty();
            // MongoDB text search will match on the indexed fields, check the id matches
            var resultIds = result.Select(p => p.Id.ToString()).ToList();
            resultIds.Should().Contain(property1.Id.ToString());
            resultIds.Should().NotContain(property2.Id.ToString());
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldReturnEmptyList_WhenNoTextMatches()
        {
            // Arrange - Reset collection
            var property1 = CreateTestProperty();
            property1.Address.Street = "123 Maple Avenue";
            await _repository.CreateAsync(property1);

            var property2 = CreateTestProperty();
            property2.Address.Street = "456 Oak Street";
            await _repository.CreateAsync(property2);
            
            // Act
            var result = await _repository.SearchPropertiesAsync(_tenantId, "cherry");
            
            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task SearchPropertiesAsync_ShouldReturnEmptyList_WhenSearchTextIsEmptyOrWhitespace(string emptySearchText)
        {
            // Act
            var result = await _repository.SearchPropertiesAsync(_tenantId, emptySearchText);
            
            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowArgumentNullException_WhenSearchTextIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.SearchPropertiesAsync(_tenantId, null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task SearchPropertiesAsync_ShouldThrowArgumentException_WhenTenantIdIsEmptyOrWhitespace(string emptyTenantId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.SearchPropertiesAsync(emptyTenantId, "test"));
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowArgumentException_WhenTenantIdIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.SearchPropertiesAsync(null!, "test"));
        }

        #endregion
    }
}
