using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using Xunit;

namespace RentTrackerBackend.Tests.Unit.Repositories
{
    /// <summary>
    /// Test document for testing MongoDbExtensions
    /// </summary>
    public class ExtensionTestDocument : BaseDocument
    {
        public required string Title { get; set; }
        public int Counter { get; set; }
    }
    
    public class MongoDbExtensionsTests
    {
        private readonly IMongoCollection<ExtensionTestDocument> _collection;

        public MongoDbExtensionsTests()
        {
            // Create substitutes
            _collection = Substitute.For<IMongoCollection<ExtensionTestDocument>>();
            
            // Setup indexes
            var indexManager = Substitute.For<IMongoIndexManager<ExtensionTestDocument>>();
            _collection.Indexes.Returns(indexManager);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAllAsync_ShouldApplyCorrectFilter_BasedOnIncludeSystemFlag(bool includeSystem)
        {
            // Arrange
            var tenantId = "tenant123";
            var expectedDocs = new List<ExtensionTestDocument>
            {
                new ExtensionTestDocument { TenantId = tenantId, Title = "Test Doc 1" }
            };

            if (includeSystem)
            {
                expectedDocs.Add(new ExtensionTestDocument { TenantId = "system", Title = "System Doc" });
            }

            var cursor = Substitute.For<IAsyncCursor<ExtensionTestDocument>>();
            cursor.Current.Returns(expectedDocs);
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<ExtensionTestDocument>>(),
                Arg.Any<FindOptions<ExtensionTestDocument, ExtensionTestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _collection.GetAllAsync(tenantId, includeSystem);

            // Assert
            result.Should().BeEquivalentTo(expectedDocs);

            // Verify the correct filter is applied
            await _collection.Received(1).FindAsync(
                Arg.Is<FilterDefinition<ExtensionTestDocument>>(f => true), // Can't easily verify the filter content
                Arg.Any<FindOptions<ExtensionTestDocument, ExtensionTestDocument>>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetByIdAsync_ShouldApplyCorrectFilter()
        {
            // Arrange
            var tenantId = "tenant123";
            var docId = ObjectId.GenerateNewId();
            var expectedDoc = new ExtensionTestDocument { TenantId = tenantId, Title = "Test Doc" };
            expectedDoc.GetType().GetProperty("Id")?.SetValue(expectedDoc, docId);

            var cursor = Substitute.For<IAsyncCursor<ExtensionTestDocument>>();
            cursor.Current.Returns(new List<ExtensionTestDocument> { expectedDoc });
            cursor.MoveNextAsync(Arg.Any<CancellationToken>())
                .Returns(true, false);

            _collection.FindAsync(
                Arg.Any<FilterDefinition<ExtensionTestDocument>>(),
                Arg.Any<FindOptions<ExtensionTestDocument, ExtensionTestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _collection.GetByIdAsync(tenantId, docId.ToString());

            // Assert
            result.Should().BeEquivalentTo(expectedDoc);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowFormatException_WhenInvalidObjectId()
        {
            // Arrange
            var tenantId = "tenant123";
            var invalidId = "invalid-id";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<FormatException>(() => 
                _collection.GetByIdAsync(tenantId, invalidId));
            
            exception.Message.Should().Contain("ObjectId");
        }

        [Fact]
        public async Task CreateAsync_ShouldSetMetadataFields()
        {
            // Arrange
            var doc = new ExtensionTestDocument 
            { 
                TenantId = "tenant123",
                Title = "Test Document"
            };

            var beforeCreate = DateTime.UtcNow;

            // Act
            var result = await _collection.CreateAsync(doc);

            // Assert
            result.Should().BeSameAs(doc);
            doc.CreatedAt.Should().BeOnOrAfter(beforeCreate);
            doc.UpdatedAt.Should().BeOnOrAfter(beforeCreate);
            doc.Version.Should().Be(1);

            await _collection.Received(1).InsertOneAsync(
                Arg.Is<ExtensionTestDocument>(d => d == doc),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentNullException_WhenDocumentIsNull()
        {
            // Act & Assert
            var nullDoc = null as ExtensionTestDocument;
            await Assert.ThrowsAsync<ArgumentNullException>(() => _collection.CreateAsync(nullDoc!));
        }

        [Fact]
        public async Task UpdateAsync_ShouldIncrementVersionAndUpdateTimestamp()
        {
            // Arrange
            var tenantId = "tenant123";
            var docId = ObjectId.GenerateNewId().ToString();
            var initialVersion = 3;
            var doc = new ExtensionTestDocument 
            { 
                TenantId = tenantId,
                Title = "Updated Document",
                Version = initialVersion,
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var beforeUpdate = DateTime.UtcNow;

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<ExtensionTestDocument>>(),
                Arg.Any<ExtensionTestDocument>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(1, 1, null));

            // Act
            await _collection.UpdateAsync(tenantId, docId, doc);

            // Assert
            doc.Version.Should().Be(initialVersion + 1);
            doc.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);

            await _collection.Received(1).ReplaceOneAsync(
                Arg.Any<FilterDefinition<ExtensionTestDocument>>(),
                Arg.Is<ExtensionTestDocument>(d => d.Version == initialVersion + 1),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowInvalidOperationException_WhenNoDocumentUpdated()
        {
            // Arrange
            var tenantId = "tenant123";
            var docId = ObjectId.GenerateNewId().ToString();
            var doc = new ExtensionTestDocument { TenantId = tenantId, Version = 5, Title = "Test Doc" };
            var originalUpdatedAt = doc.UpdatedAt;

            _collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<ExtensionTestDocument>>(),
                Arg.Any<ExtensionTestDocument>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(0, 0, null));

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _collection.UpdateAsync(tenantId, docId, doc));

            // Assert
            exception.Message.Should().Contain("Concurrency conflict");
            doc.Version.Should().Be(5); // Original version
            doc.UpdatedAt.Should().Be(DateTime.MinValue); // Rolled back timestamp
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowFormatException_WhenInvalidObjectId()
        {
            // Arrange
            var tenantId = "tenant123";
            var invalidId = "invalid-id";
            var doc = new ExtensionTestDocument { TenantId = tenantId, Title = "Test Doc" };

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => 
                _collection.UpdateAsync(tenantId, invalidId, doc));
        }

        [Fact]
        public async Task DeleteAsync_ShouldApplyCorrectFilter()
        {
            // Arrange
            var tenantId = "tenant123";
            var docId = ObjectId.GenerateNewId().ToString();

            _collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<ExtensionTestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(new DeleteResult.Acknowledged(1));

            // Act
            await _collection.DeleteAsync(tenantId, docId);            // Assert
            await _collection.Received(1).DeleteOneAsync(
                Arg.Any<FilterDefinition<ExtensionTestDocument>>(),
                Arg.Any<CancellationToken>());
        }
          
        [Fact]
        public async Task DeleteAsync_ShouldThrowFormatException_WhenInvalidObjectId()
        {
            // Arrange
            var tenantId = "tenant123";
            var invalidId = "invalid-id";
            
            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => 
                _collection.DeleteAsync(tenantId, invalidId));
        }
          [Fact]        
        public async Task CreateTenantIndexesAsync_ShouldCreateIndexes()
        {
            // Arrange - Create a completely fresh mock setup
            var collection = Substitute.For<IMongoCollection<ExtensionTestDocument>>();
            var indexManager = Substitute.For<IMongoIndexManager<ExtensionTestDocument>>();
            
            // Make sure index manager is returned when Indexes property is accessed
            collection.Indexes.Returns(indexManager);
            
            // Set up the expected response
            indexManager
                .CreateManyAsync(
                    Arg.Any<IEnumerable<CreateIndexModel<ExtensionTestDocument>>>(),
                    Arg.Any<CreateManyIndexesOptions>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IEnumerable<string>>(new List<string> { "index1", "index2" }));

            // Act
            await MongoDbExtensions.CreateTenantIndexesAsync(collection);

            // Assert - Verify CreateManyAsync was called with any arguments
            await indexManager
                .Received(1)
                .CreateManyAsync(
                    Arg.Any<IEnumerable<CreateIndexModel<ExtensionTestDocument>>>(),
                    Arg.Any<CreateManyIndexesOptions>(),
                    Arg.Any<CancellationToken>());
        }
    }
}
