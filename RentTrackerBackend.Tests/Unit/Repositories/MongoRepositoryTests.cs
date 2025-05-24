using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Tests.Unit.Repositories
{
    /// <summary>
    /// Test entity representing a generic document for testing the MongoRepository
    /// </summary>
    public class TestDocument : BaseDocument
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class MongoRepositoryTests
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<TestDocument> _collection;
        private readonly MongoRepository<TestDocument> _repository;
        private readonly string _collectionName = "TestDocument";

        public MongoRepositoryTests()
        {
            // Create substitutes
            _database = Substitute.For<IMongoDatabase>();
            _collection = Substitute.For<IMongoCollection<TestDocument>>();

            // Configure database
            _database.GetCollection<TestDocument>(_collectionName).Returns(_collection);

            // Setup indexes
            var indexManager = Substitute.For<IMongoIndexManager<TestDocument>>();
            _collection.Indexes.Returns(indexManager);

            // Create repository
            _repository = new MongoRepository<TestDocument>(_database);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllDocuments_WhenDocumentsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var expectedDocuments = new List<TestDocument>
            {
                new TestDocument { TenantId = tenantId, Name = "Test1", Value = 100 },
                new TestDocument { TenantId = tenantId, Name = "Test2", Value = 200 }
            };

            var cursor = Substitute.For<IAsyncCursor<TestDocument>>();
            cursor.Current.Returns(expectedDocuments);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<FindOptions<TestDocument, TestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEquivalentTo(expectedDocuments);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoDocumentsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var cursor = Substitute.For<IAsyncCursor<TestDocument>>();
            cursor.Current.Returns(new List<TestDocument>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<FindOptions<TestDocument, TestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ShouldIncludeSystemDocuments_WhenIncludeSystemIsTrue()
        {
            // Arrange
            var tenantId = "tenant123";
            var expectedDocuments = new List<TestDocument>
            {
                new TestDocument { TenantId = tenantId, Name = "Tenant Doc" },
                new TestDocument { TenantId = "system", Name = "System Doc" }
            };

            var cursor = Substitute.For<IAsyncCursor<TestDocument>>();
            cursor.Current.Returns(expectedDocuments);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<FindOptions<TestDocument, TestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId, true);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(d => d.TenantId == "system");
        }

        [Fact]
        public async Task GetAllAsync_ShouldThrowMongoException_WhenDatabaseError()
        {
            // Arrange
            var tenantId = "tenant123";
            _collection.FindAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<FindOptions<TestDocument, TestDocument>>(),
                Arg.Any<CancellationToken>())
                .ThrowsAsync(new MongoException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() => _repository.GetAllAsync(tenantId));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDocument_WhenDocumentExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var documentId = ObjectId.GenerateNewId();
            var expectedDocument = new TestDocument { TenantId = tenantId, Name = "Test Doc" };
            expectedDocument.GetType().GetProperty("Id")?.SetValue(expectedDocument, documentId);

            var cursor = Substitute.For<IAsyncCursor<TestDocument>>();
            cursor.Current.Returns(new List<TestDocument> { expectedDocument });
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<FindOptions<TestDocument, TestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, documentId.ToString());

            // Assert
            result.Should().BeEquivalentTo(expectedDocument);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDocumentDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var documentId = ObjectId.GenerateNewId().ToString();

            var cursor = Substitute.For<IAsyncCursor<TestDocument>>();
            cursor.Current.Returns(new List<TestDocument>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<FindOptions<TestDocument, TestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, documentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowFormatException_WhenInvalidObjectId()
        {
            // Arrange
            var tenantId = "tenant123";
            var invalidId = "not-a-valid-objectid";

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => _repository.GetByIdAsync(tenantId, invalidId));
        }

        [Fact]
        public async Task CreateAsync_ShouldInsertDocument_WhenValidDocumentProvided()
        {
            // Arrange
            var document = new TestDocument 
            { 
                TenantId = "tenant123",
                Name = "New Document",
                Value = 42
            };

            // Act
            await _repository.CreateAsync(document);

            // Assert
            await _collection.Received(1).InsertOneAsync(
                Arg.Is<TestDocument>(d => d.TenantId == document.TenantId && d.Name == document.Name),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_ShouldSetCreatedAndUpdatedDates_WhenDocumentCreated()
        {
            // Arrange
            var document = new TestDocument { TenantId = "tenant123", Name = "Test Doc" };
            var beforeCreate = DateTime.UtcNow;

            // Act
            await _repository.CreateAsync(document);

            // Assert
            document.CreatedAt.Should().BeAfter(beforeCreate);
            document.UpdatedAt.Should().BeAfter(beforeCreate);
            document.Version.Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentNullException_WhenDocumentIsNull()
        {
            // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.CreateAsync(null));
#pragma warning restore CS8625
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateDocument_WhenDocumentExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var documentId = ObjectId.GenerateNewId().ToString();
            var document = new TestDocument 
            { 
                TenantId = tenantId,
                Name = "Updated Document",
                Version = 1
            };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<TestDocument>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(1, 1, null));

            // Act
            await _repository.UpdateAsync(tenantId, documentId, document);

            // Assert
            await _collection.Received(1).ReplaceOneAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Is<TestDocument>(d => d.Version == 2),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_ShouldIncrementVersion_WhenDocumentUpdated()
        {
            // Arrange
            var tenantId = "tenant123";
            var documentId = ObjectId.GenerateNewId().ToString();
            var currentVersion = 5;
            var document = new TestDocument { TenantId = tenantId, Version = currentVersion };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<TestDocument>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(1, 1, null));

            // Act
            await _repository.UpdateAsync(tenantId, documentId, document);

            // Assert
            document.Version.Should().Be(currentVersion + 1);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateTimestamp_WhenDocumentUpdated()
        {
            // Arrange
            var tenantId = "tenant123";
            var documentId = ObjectId.GenerateNewId().ToString();
            var document = new TestDocument { 
                TenantId = tenantId,
                UpdatedAt = DateTime.UtcNow.AddDays(-1) // Old timestamp
            };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<TestDocument>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(1, 1, null));

            var beforeUpdate = DateTime.UtcNow;

            // Act
            await _repository.UpdateAsync(tenantId, documentId, document);

            // Assert
            document.UpdatedAt.Should().BeAfter(beforeUpdate);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowFormatException_WhenInvalidObjectId()
        {
            // Arrange
            var tenantId = "tenant123";
            var invalidId = "not-a-valid-objectid";
            var document = new TestDocument { TenantId = tenantId };

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => _repository.UpdateAsync(tenantId, invalidId, document));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowInvalidOperationException_WhenDocumentNotFound()
        {
            // Arrange
            var tenantId = "tenant123";
            var documentId = ObjectId.GenerateNewId().ToString();
            var document = new TestDocument { TenantId = tenantId };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<TestDocument>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(0, 0, null));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.UpdateAsync(tenantId, documentId, document));
        }

        [Fact]
        public async Task UpdateAsync_ShouldRollbackChanges_WhenConcurrencyConflict()
        {
            // Arrange
            var tenantId = "tenant123";
            var documentId = ObjectId.GenerateNewId().ToString();
            var originalVersion = 3;
            var originalUpdatedAt = DateTime.UtcNow.AddHours(-1);
            
            var document = new TestDocument { 
                TenantId = tenantId,
                Version = originalVersion,
                UpdatedAt = originalUpdatedAt
            };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<TestDocument>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(0, 0, null));

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.UpdateAsync(tenantId, documentId, document));

            // Assert
            exception.Message.Should().Contain("Concurrency conflict");
            document.Version.Should().Be(originalVersion);
            document.UpdatedAt.Should().Be(DateTime.MinValue);  // Rolled back timestamp
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteDocument_WhenDocumentExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var documentId = ObjectId.GenerateNewId().ToString();

            _collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(new DeleteResult.Acknowledged(1));

            // Act
            await _repository.DeleteAsync(tenantId, documentId);

            // Assert
            await _collection.Received(1).DeleteOneAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_ShouldNotThrowException_WhenDocumentDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var documentId = ObjectId.GenerateNewId().ToString();

            _collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<TestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(new DeleteResult.Acknowledged(0));

            // Act & Assert - Should not throw exception
            await _repository.DeleteAsync(tenantId, documentId);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowFormatException_WhenInvalidObjectId()
        {
            // Arrange
            var tenantId = "tenant123";
            var invalidId = "not-a-valid-objectid";

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => _repository.DeleteAsync(tenantId, invalidId));
        }
    }
}
