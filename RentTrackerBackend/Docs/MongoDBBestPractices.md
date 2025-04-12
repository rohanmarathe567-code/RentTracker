# MongoDB Best Practices Guide

## Data Model Guidelines

### Base Document Structure
- All documents inherit from `BaseDocument`
- Required fields: `Id`, `TenantId`, `CreatedAt`, `UpdatedAt`, `Version`
- Use UTC timestamps for all date fields

### Data Types
- Use `ObjectId` for document IDs
- Store dates as `DateTime` (UTC)
- Use appropriate numeric types (`decimal` for currency)

## Indexing Strategy

### Core Indexes
1. TenantId (required for multi-tenancy)
2. Compound indexes for frequently accessed fields
3. Text indexes for search functionality
4. Version field for optimistic concurrency

### Property Collection Indexes
```csharp
// Example index definitions
new CreateIndexModel<RentalProperty>(
    indexBuilder.Ascending(x => x.TenantId)
              .Ascending(x => x.RentAmount))

new CreateIndexModel<RentalProperty>(
    indexBuilder.Text("Address.Street")
              .Text("Address.City")
              .Text("Address.State"))
```

## Query Optimization

### Best Practices
1. Always include TenantId in queries
2. Use compound indexes for range queries
3. Implement pagination for large result sets
4. Use projection to limit returned fields

### Example Queries
```csharp
// Good: Using compound index
await _collection.Find(x => 
    x.TenantId == tenantId && 
    x.RentAmount >= minRent)
    .Project<T>(...)
    .Skip(offset)
    .Limit(pageSize)
    .ToListAsync();

// Bad: Not using indexes
await _collection.Find(x => 
    x.Address.Street.Contains(searchText))
    .ToListAsync();
```

## Concurrency Control

### Optimistic Concurrency
- Use Version field for concurrency control
- Increment Version on each update
- Handle version conflicts appropriately

```csharp
// Example version check
var result = await _collection.ReplaceOneAsync(
    x => x.Id == id && x.Version == currentVersion,
    entity);

if (result.ModifiedCount == 0)
{
    throw new InvalidOperationException("Concurrency conflict");
}
```

## Caching Strategy

### Redis Integration
- Cache frequently accessed data
- Use appropriate cache expiry times
- Implement cache invalidation on updates

```csharp
// Example cache key format
$"{tenantId}:property:{propertyId}"

// Cache duration
TimeSpan.FromMinutes(30)
```

## Error Handling

### Common Scenarios
1. Connection failures
2. Concurrency conflicts
3. Validation errors
4. Index constraints

### Best Practices
- Use meaningful exception messages
- Implement retry logic for transient failures
- Log detailed error information
- Handle version conflicts gracefully

## Performance Monitoring

### Key Metrics
1. Query execution time
2. Index usage statistics
3. Cache hit/miss ratios
4. Document size growth

### Tools
- MongoDB Compass for development
- Application Insights integration
- Log slow queries for analysis

## Security Considerations

### Data Access
- Always filter by TenantId
- Use parameter binding to prevent injection
- Implement proper access controls
- Use secure connection strings

### Validation
- Implement model validation
- Sanitize input data
- Validate document size limits

## Backup and Recovery

### Development Environment
- Daily snapshots
- Weekly full backups
- Backup before major changes

### Production Environment
- Continuous backup
- Point-in-time recovery
- Geographic redundancy