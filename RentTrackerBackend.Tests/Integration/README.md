# Integration Tests

This directory contains integration tests that verify component interactions and external dependencies.

## Subdirectories

### Database/
Tests for MongoDB integration:
- MongoDB test container setup and configuration
- Schema validation and indexing
- Complex queries and aggregation pipelines
- Data consistency across operations
- Connection pool management
- Performance with large datasets

### API/
Tests for complete API request flows:
- In-memory API testing using WebApplicationFactory
- End-to-end request processing
- Database interactions
- Authentication flows
- Error handling scenarios
- Response formatting
- Content negotiation

## Key Features
- Uses test containers for MongoDB
- Real database interactions
- Complete request-to-database flows
- Authentication and authorization testing
- Performance monitoring

## Best Practices
- Clean up test data after each test
- Use realistic test scenarios
- Monitor performance metrics
- Handle async operations properly
- Isolate test data between runs