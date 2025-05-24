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
    public class AttachmentRepositoryTests
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Attachment> _collection;
        private readonly IOptions<MongoDbSettings> _settings;
        private readonly AttachmentRepository _repository;
        private readonly string _databaseName = "testdb";

        public AttachmentRepositoryTests()
        {
            // Create substitutes
            _mongoClient = Substitute.For<IMongoClient>();
            _database = Substitute.For<IMongoDatabase>();
            _collection = Substitute.For<IMongoCollection<Attachment>>();
            _settings = Substitute.For<IOptions<MongoDbSettings>>();

            // Configure settings
            _settings.Value.Returns(new MongoDbSettings { 
                DatabaseName = _databaseName,
                ConnectionString = "mongodb://localhost:27017"
            });

            // Configure database
            _mongoClient.GetDatabase(_databaseName).Returns(_database);
            _database.GetCollection<Attachment>(typeof(Attachment).Name).Returns(_collection);

            // Setup indexes
            var indexManager = Substitute.For<IMongoIndexManager<Attachment>>();
            _collection.Indexes.Returns(indexManager);

            // Create repository
            _repository = new AttachmentRepository(_mongoClient, _settings);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllAttachments_WhenAttachmentsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var expectedAttachments = new List<Attachment>
            {
                new Attachment { TenantId = tenantId },
                new Attachment { TenantId = tenantId }
            };

            var cursor = Substitute.For<IAsyncCursor<Attachment>>();
            cursor.Current.Returns(expectedAttachments);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<FindOptions<Attachment, Attachment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAllAsync(tenantId);

            // Assert
            result.Should().BeEquivalentTo(expectedAttachments);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoAttachmentsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var cursor = Substitute.For<IAsyncCursor<Attachment>>();
            cursor.Current.Returns(new List<Attachment>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<FindOptions<Attachment, Attachment>>(),
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
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<FindOptions<Attachment, Attachment>>(),
                Arg.Any<CancellationToken>())
                .ThrowsAsync(new MongoException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() => _repository.GetAllAsync(tenantId));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnAttachment_WhenAttachmentExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var attachmentId = ObjectId.GenerateNewId();
            var expectedAttachment = new Attachment { TenantId = tenantId };
            expectedAttachment.GetType().GetProperty("Id")?.SetValue(expectedAttachment, attachmentId);

            var cursor = Substitute.For<IAsyncCursor<Attachment>>();
            cursor.Current.Returns(new List<Attachment> { expectedAttachment });
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<FindOptions<Attachment, Attachment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, attachmentId.ToString());

            // Assert
            result.Should().BeEquivalentTo(expectedAttachment);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenAttachmentDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var attachmentId = ObjectId.GenerateNewId().ToString();

            var cursor = Substitute.For<IAsyncCursor<Attachment>>();
            cursor.Current.Returns(new List<Attachment>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<FindOptions<Attachment, Attachment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetByIdAsync(tenantId, attachmentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldInsertAttachment_WhenValidAttachmentProvided()
        {
            // Arrange
            var attachment = new Attachment 
            { 
                TenantId = "tenant123",
                FileName = "test.pdf",
                StoragePath = "/uploads/test.pdf",
                FileSize = 1024,
                ContentType = "application/pdf",
                EntityType = "Property"
            };

            _collection.InsertOneAsync(
                Arg.Any<Attachment>(),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _repository.CreateAsync(attachment);

            // Assert
            await _collection.Received(1).InsertOneAsync(
                Arg.Is<Attachment>(a => a.TenantId == attachment.TenantId),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>());
            
            result.Should().BeEquivalentTo(attachment);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentNullException_WhenAttachmentIsNull()
        {
            // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.CreateAsync(null));
#pragma warning restore CS8625
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateAttachment_WhenAttachmentExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var attachmentId = ObjectId.GenerateNewId().ToString();
            var attachment = new Attachment { 
                TenantId = tenantId,
                FileName = "updated.pdf"
            };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<Attachment>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(1, 1, null));

            // Act
            await _repository.UpdateAsync(tenantId, attachmentId, attachment);

            // Assert
            await _collection.Received(1).ReplaceOneAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<Attachment>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowInvalidOperationException_WhenAttachmentNotFound()
        {
            // Arrange
            var tenantId = "tenant123";
            var attachmentId = ObjectId.GenerateNewId().ToString();
            var attachment = new Attachment { TenantId = tenantId };

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<Attachment>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(0, 0, null));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.UpdateAsync(tenantId, attachmentId, attachment));
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteAttachment_WhenAttachmentExists()
        {
            // Arrange
            var tenantId = "tenant123";
            var attachmentId = ObjectId.GenerateNewId().ToString();

            _collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<CancellationToken>())
                .Returns(new DeleteResult.Acknowledged(1));

            // Act
            await _repository.DeleteAsync(tenantId, attachmentId);

            // Assert
            await _collection.Received(1).DeleteOneAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_ShouldNotThrowException_WhenAttachmentDoesNotExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var attachmentId = ObjectId.GenerateNewId().ToString();

            _collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<CancellationToken>())
                .Returns(new DeleteResult.Acknowledged(0));

            // Act & Assert
            await _repository.DeleteAsync(tenantId, attachmentId);
            // Should complete without throwing an exception
        }

        [Fact]
        public async Task GetAttachmentsByPropertyIdAsync_ShouldReturnAttachments_WhenAttachmentsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = "property456";
            var expectedAttachments = new List<Attachment>
            {
                new Attachment { TenantId = tenantId, RentalPropertyId = propertyId },
                new Attachment { TenantId = tenantId, RentalPropertyId = propertyId }
            };

            var cursor = Substitute.For<IAsyncCursor<Attachment>>();
            cursor.Current.Returns(expectedAttachments);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<FindOptions<Attachment, Attachment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAttachmentsByPropertyIdAsync(tenantId, propertyId);

            // Assert
            result.Should().BeEquivalentTo(expectedAttachments);
        }

        [Fact]
        public async Task GetAttachmentsByPropertyIdAsync_ShouldReturnEmptyList_WhenNoAttachmentsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var propertyId = "property456";

            var cursor = Substitute.For<IAsyncCursor<Attachment>>();
            cursor.Current.Returns(new List<Attachment>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<FindOptions<Attachment, Attachment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAttachmentsByPropertyIdAsync(tenantId, propertyId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAttachmentsByEntityTypeAsync_ShouldReturnAttachments_WhenAttachmentsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var entityType = "Property";
            var expectedAttachments = new List<Attachment>
            {
                new Attachment { TenantId = tenantId, EntityType = entityType },
                new Attachment { TenantId = tenantId, EntityType = entityType }
            };

            var cursor = Substitute.For<IAsyncCursor<Attachment>>();
            cursor.Current.Returns(expectedAttachments);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<FindOptions<Attachment, Attachment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAttachmentsByEntityTypeAsync(tenantId, entityType);

            // Assert
            result.Should().BeEquivalentTo(expectedAttachments);
        }

        [Fact]
        public async Task GetAttachmentsByEntityTypeAsync_ShouldReturnEmptyList_WhenNoAttachmentsExist()
        {
            // Arrange
            var tenantId = "tenant123";
            var entityType = "Property";

            var cursor = Substitute.For<IAsyncCursor<Attachment>>();
            cursor.Current.Returns(new List<Attachment>());
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<Attachment>>(),
                Arg.Any<FindOptions<Attachment, Attachment>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.GetAttachmentsByEntityTypeAsync(tenantId, entityType);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetAllAsync_ShouldThrowArgumentException_WhenTenantIdIsInvalid(string tenantId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetAllAsync(tenantId));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenTenantIdIsInvalid(string tenantId)
        {
            // Arrange
            var attachmentId = ObjectId.GenerateNewId().ToString();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetByIdAsync(tenantId, attachmentId));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenIdIsInvalid(string id)
        {
            // Arrange
            var tenantId = "tenant123";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetByIdAsync(tenantId, id));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetAttachmentsByPropertyIdAsync_ShouldThrowArgumentException_WhenPropertyIdIsInvalid(string propertyId)
        {
            // Arrange
            var tenantId = "tenant123";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetAttachmentsByPropertyIdAsync(tenantId, propertyId));
        }


        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetAttachmentsByEntityTypeAsync_ShouldThrowArgumentException_WhenEntityTypeIsInvalid(string entityType)
        {
            // Arrange
            var tenantId = "tenant123";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _repository.GetAttachmentsByEntityTypeAsync(tenantId, entityType));
        }
    }
}
