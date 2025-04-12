# Developer Guidelines

## MongoDB Development Guidelines

### Setting Up Development Environment

1. **Docker Setup**
```bash
docker-compose up -d mongodb redis
```

2. **Required NuGet Packages**
```xml
<PackageReference Include="MongoDB.Driver" Version="2.22.0" />
<PackageReference Include="MongoDB.Bson" Version="2.22.0" />
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="7.0.0" />
```

### Working with MongoDB Models

1. **Base Document Structure**
```csharp
public class BaseDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Version { get; set; }
}
```

2. **Model Requirements**
- Always inherit from BaseDocument
- Use appropriate MongoDB attributes
- Include version field for concurrency
- Use UTC for all dates

### Repository Pattern Usage

1. **Basic Repository Operations**
```csharp
// Fetching data
var items = await _repository.GetAllAsync(tenantId);
var item = await _repository.GetByIdAsync(tenantId, id);

// Creating
var newItem = new MyModel { ... };
await _repository.CreateAsync(newItem);

// Updating with concurrency
try {
    await _repository.UpdateAsync(tenantId, id, updatedItem);
} catch (InvalidOperationException ex) {
    // Handle concurrency conflict
}

// Deleting
await _repository.DeleteAsync(tenantId, id);
```

2. **Custom Repository Methods**
- Inherit from MongoRepository<T>
- Add specific business logic methods
- Maintain multi-tenancy filters
- Use proper indexing

### Query Guidelines

1. **Efficient Queries**
```csharp
// Good: Using indexes
var result = await _collection
    .Find(x => x.TenantId == tenantId)
    .Project<T>(projection)
    .Skip(offset)
    .Limit(pageSize)
    .ToListAsync();

// Bad: Not using indexes
var result = await _collection
    .Find(x => x.SomeUnindexedField == value)
    .ToListAsync();
```

2. **Text Search**
```csharp
var filter = Builders<T>.Filter.Text(searchTerm);
var results = await _collection
    .Find(filter)
    .SortByDescending(x => x.UpdatedAt)
    .ToListAsync();
```

### Caching Best Practices

1. **Cache Key Format**
```csharp
// Single item
$"{tenantId}:entityName:{id}"

// Collection
$"{tenantId}:entityName:list:{parameters}"
```

2. **Cache Duration**
- Short-lived data: 5-15 minutes
- Static data: 30-60 minutes
- Clear cache on updates

### Error Handling

1. **MongoDB Specific Errors**
```csharp
try {
    await operation();
} catch (MongoCommandException ex) {
    // Handle MongoDB specific errors
} catch (MongoConnectionException ex) {
    // Handle connection issues
} catch (InvalidOperationException ex) {
    // Handle concurrency conflicts
}
```

2. **Logging**
```csharp
logger.LogInformation("Operation started for {TenantId}", tenantId);
logger.LogError(ex, "Failed to save document: {Message}", ex.Message);
```

### Testing Guidelines

1. **Unit Tests**
- Use MongoDB.Driver.Core.TestHelpers
- Mock MongoDB interfaces
- Test concurrency scenarios
- Verify index usage

2. **Integration Tests**
- Use MongoDB container
- Clean database between tests
- Test with real data
- Verify cache behavior

### Performance Optimization

1. **Index Usage**
- Monitor index usage with MongoDB Compass
- Create indexes for common queries
- Remove unused indexes
- Use compound indexes effectively

2. **Query Optimization**
- Use projections to limit returned fields
- Implement pagination
- Batch operations when possible
- Monitor query performance

### Deployment Considerations

1. **Configuration**
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "RentTracker"
  }
}
```

2. **Monitoring**
- Set up MongoDB monitoring
- Watch for slow queries
- Monitor index usage
- Track cache hit rates

### Code Review Checklist

1. **Data Access**
- [ ] Proper inheritance from BaseDocument
- [ ] Correct index definitions
- [ ] Multi-tenancy enforcement
- [ ] Optimistic concurrency handling

2. **Performance**
- [ ] Efficient query patterns
- [ ] Appropriate indexing
- [ ] Proper caching implementation
- [ ] Pagination for large results

3. **Security**
- [ ] TenantId filtering
- [ ] Input validation
- [ ] Proper error handling
- [ ] Secure configuration

4. **Testing**
- [ ] Unit tests for repository
- [ ] Integration tests
- [ ] Concurrency tests
- [ ] Cache behavior tests

### Troubleshooting Guide

1. **Common Issues**
- Concurrency conflicts
- Missing indexes
- Cache inconsistencies
- Connection problems

2. **Diagnostic Tools**
- MongoDB Compass
- Application logs
- Performance metrics
- Cache statistics

3. **Resolution Steps**
- Check logs for errors
- Verify index usage
- Monitor query performance
- Test in isolation

### Development Workflow

1. **New Features**
- Design data model
- Plan indexes
- Implement repository
- Add caching strategy
- Write tests
- Document API

2. **Code Changes**
- Review existing indexes
- Check cache impact
- Update documentation
- Run tests
- Monitor performance

Remember: Always consider multi-tenancy, performance, and data consistency when making changes.