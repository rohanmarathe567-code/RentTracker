# MongoDB Migration Implementation Plan

## Overview
This document outlines the step-by-step plan for migrating RentTracker from PostgreSQL to MongoDB, focusing on implementing a flexible schema design that supports future extensibility.

## 1. Development Environment Setup

### MongoDB Setup
```bash
# Docker compose addition
services:
  mongodb:
    image: mongo:latest
    ports:
      - "27017:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: root
      MONGO_INITDB_ROOT_PASSWORD: example
    volumes:
      - mongodb_data:/data/db
```

### Required NuGet Packages
- MongoDB.Driver
- MongoDB.Bson
- Microsoft.Extensions.Options.ConfigurationExtensions

## 2. Data Model Implementation

### Base Document Classes
```csharp
public class BaseDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RentalProperty : BaseDocument
{
    public Address Address { get; set; }
    public decimal RentAmount { get; set; }
    public LeaseDates LeaseDates { get; set; }
    public List<Payment> Payments { get; set; }
    public List<Attachment> Attachments { get; set; }
    public Dictionary<string, object> Attributes { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
}

public class LeaseDates
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
```

## 3. Database Configuration

### Configuration Setup
```csharp
public class MongoDbSettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
}

// Program.cs
services.Configure<MongoDbSettings>(configuration.GetSection("MongoDb"));
services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
```

## 4. Repository Implementation

### Base Repository
```csharp
public interface IMongoRepository<T> where T : BaseDocument
{
    Task<IEnumerable<T>> GetAllAsync(string tenantId);
    Task<T> GetByIdAsync(string tenantId, string id);
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(string tenantId, string id, T entity);
    Task DeleteAsync(string tenantId, string id);
}

public class MongoRepository<T> : IMongoRepository<T> where T : BaseDocument
{
    private readonly IMongoCollection<T> _collection;

    public MongoRepository(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<T>(typeof(T).Name);
        
        // Create indexes
        var indexBuilder = Builders<T>.IndexKeys;
        var indexes = new List<CreateIndexModel<T>>
        {
            new CreateIndexModel<T>(indexBuilder.Ascending(x => x.TenantId)),
            new CreateIndexModel<T>(indexBuilder.Ascending(x => x.TenantId).Ascending("Address.City"))
        };
        _collection.Indexes.CreateMany(indexes);
    }
}
```

## 5. Integration with Redis Cache

### Cache Key Strategy
```csharp
public static class CacheKeys
{
    public static string GetPropertyKey(string tenantId, string propertyId) 
        => $"{tenantId}:property:{propertyId}";
}
```

## 6. Implementation Phases

### Phase 1: Basic Setup (1 week)
- [ ] Set up MongoDB in development environment
- [ ] Implement base document models
- [ ] Create basic repository implementation
- [ ] Configure MongoDB connection

### Phase 2: Core Implementation (2 weeks)
- [ ] Implement property repository
- [ ] Update API endpoints
- [ ] Integrate Redis caching
- [ ] Add MongoDB indexing

### Phase 3: Testing & Optimization (1 week)
- [ ] Unit tests for MongoDB operations
- [ ] Integration tests
- [ ] Performance testing
- [ ] Index optimization

### Phase 4: Documentation & Cleanup (1 week)
- [ ] API documentation updates
- [ ] MongoDB best practices documentation
- [ ] Code cleanup and optimization
- [ ] Developer guidelines

## 7. MongoDB Best Practices

### Indexing Strategy
- Compound index on TenantId + frequently queried fields
- Text indexes for search functionality
- Index for geospatial queries (future feature)

### Query Optimization
- Use projection to limit returned fields
- Implement pagination for large result sets
- Use aggregation pipeline for complex queries

### Data Consistency
- Implement optimistic concurrency using version field
- Use transactions for complex operations
- Regular data validation checks

## 8. Monitoring & Maintenance

### Key Metrics
- Query performance
- Index usage statistics
- Document size growth
- Cache hit ratios

### Tools
- MongoDB Compass for development
- MongoDB Atlas monitoring (if using cloud)
- Application insights integration

## 9. Backup Strategy

### Development
- Daily snapshots
- Weekly full backups
- Backup before major changes

### Production (Future)
- Continuous backup
- Point-in-time recovery
- Geographic redundancy

## Next Steps
1. Review this plan with the development team
2. Set up MongoDB in development environment
3. Begin Phase 1 implementation
4. Schedule regular progress reviews

## Success Criteria
- All existing functionality working with MongoDB
- Query performance meeting or exceeding current performance
- Successful implementation of flexible attributes
- All tests passing
- Documentation complete