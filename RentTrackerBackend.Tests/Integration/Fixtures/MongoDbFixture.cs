using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Mongo2Go;
using RentTrackerBackend.Models;
using System;
using Xunit;

namespace RentTrackerBackend.Tests.Integration.Fixtures
{
    public class MongoDbFixture : IAsyncDisposable
    {
        private readonly MongoDbRunner _runner;
        public IMongoClient MongoClient { get; private set; }
        public IMongoDatabase Database { get; private set; }
        public IOptions<MongoDbSettings> Settings { get; private set; }
        public string DatabaseName { get; } = "RentTrackerTestDb";

        private MongoDbFixture(MongoDbRunner runner, IMongoClient client, IMongoDatabase database, IOptions<MongoDbSettings> settings)
        {
            _runner = runner;
            MongoClient = client;
            Database = database;
            Settings = settings;
        }

        public static async Task<MongoDbFixture> CreateAsync()
        {
            string connectionString;
            MongoDbRunner runner = null!;

            try
            {
                connectionString = Environment.GetEnvironmentVariable("MongoDB__ConnectionString")!;
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Always use Mongo2Go for tests
                    runner = MongoDbRunner.Start();
                    connectionString = runner.ConnectionString;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to start Mongo2Go. If running in container, ensure MongoDB binaries can be executed.", ex);
            }

            // Configure MongoDB client with extended timeout for container environments
            var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromMinutes(1);
            var client = new MongoClient(clientSettings);
            var database = client.GetDatabase("RentTrackerTestDb");

            // Create settings
            var settings = Options.Create(new MongoDbSettings
            {
                ConnectionString = connectionString,
                DatabaseName = "RentTrackerTestDb"
            });

            var fixture = new MongoDbFixture(runner, client, database, settings);
            
            // Create required indexes
            await fixture.CreateIndexesAsync();

            return fixture;
        }


        public async Task CleanupCollectionAsync<T>(string collectionName, FilterDefinition<T>? filter = null)
        {
            try
            {
                var collection = Database.GetCollection<T>(collectionName);
                // Delete all documents in the collection
                await collection.DeleteManyAsync(Builders<T>.Filter.Empty);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to cleanup collection {collectionName}", ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_runner != null)
            {
                _runner.Dispose();
            }
            await Task.CompletedTask;
        }

        public async Task CreateIndexesAsync()
        {
            var collectionName = "properties";
            var database = MongoClient.GetDatabase(DatabaseName);

            // Create collection if it doesn't exist
            if (!await (await database.ListCollectionNamesAsync()).ToListAsync().ContinueWith(t => t.Result.Contains(collectionName)))
            {
                await database.CreateCollectionAsync(collectionName);
            }

            // Create property-specific indexes
            var collection = database.GetCollection<RentalProperty>(collectionName);
            var indexBuilder = Builders<RentalProperty>.IndexKeys;
            var indexes = new List<CreateIndexModel<RentalProperty>>
            {
                // Optimized compound indexes for common queries
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Ascending(x => x.TenantId)
                               .Ascending(x => x.RentAmount)),
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Ascending(x => x.TenantId)
                               .Ascending("LeaseDates.StartDate")
                               .Ascending("LeaseDates.EndDate")),

                // Text index for search functionality
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Text("Address.Street")
                               .Text("Address.City")
                               .Text("Address.State")),

                // Index for date-based queries
                new CreateIndexModel<RentalProperty>(
                    indexBuilder.Ascending(x => x.TenantId)
                               .Ascending(x => x.UpdatedAt))
            };

            // Create all indexes
            await collection.Indexes.CreateManyAsync(indexes);
        }
    }

    /// <summary>
    /// Collection fixture for MongoDB integration tests.
    /// This class manages a single MongoDB instance for all tests in the collection.
    /// </summary>
    [CollectionDefinition("MongoDB")]
    public class MongoDbCollection
    {
        // Singleton instance for the test session
        private static MongoDbFixture? _instance;
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private static bool _initialized;

        public static async Task<MongoDbFixture> GetInstanceAsync()
        {
            if (!_initialized)
            {
                await _lock.WaitAsync();
                try
                {
                    if (!_initialized)
                    {
                        _instance = await MongoDbFixture.CreateAsync();
                        _initialized = true;

                        // Register cleanup when process exits
                        AppDomain.CurrentDomain.ProcessExit += async (s, e) =>
                        {
                            await DisposeInstanceAsync();
                        };
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }
            return _instance ?? throw new InvalidOperationException("MongoDB instance not initialized");
        }

        public static async ValueTask DisposeInstanceAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_instance != null)
                {
                    await _instance.DisposeAsync();
                    _instance = null;
                    _initialized = false;
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
