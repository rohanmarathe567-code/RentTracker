# Unit Tests

This directory contains unit tests that verify individual components in isolation.

## Subdirectories

### Repositories/
Tests for data access layer components:
- Generic MongoDB repository operations
- Entity-specific repository operations (Property, Attachment)
- Query filters and projections
- Data validation

### Services/
Tests for business logic components:
- Authentication service
- File service
- Property service
- Business rule validation

### Endpoints/
Tests for API endpoint handlers:
- Request processing
- Response formatting
- Input validation
- Error handling

## Best Practices
- Mock external dependencies
- Focus on single component behavior
- Keep tests fast and isolated
- Follow Arrange-Act-Assert pattern