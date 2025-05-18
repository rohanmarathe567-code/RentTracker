# End-to-End Tests

This directory contains end-to-end tests that verify complete business workflows and user scenarios.

## Subdirectories

### Workflows/
Tests for complete business processes:
- Property management workflows
  * Property creation → attachment upload
  * Property updates
  * Property deletion with cascading effects
- User management workflows
  * Registration → login → profile management
  * Role assignments and permissions

### TestData/
Test data builders and utilities:
- Entity builders (Property, User, Attachment)
- Test data generation utilities
- Data cleanup routines
- Test scenario helpers

## Key Features
- Complete workflow coverage
- Realistic user scenarios
- Data consistency verification
- Cross-entity relationships
- Error handling scenarios

## Best Practices
- Use builder patterns for test data
- Clean up test data after scenarios
- Test realistic user workflows
- Verify system state after operations
- Document test scenarios clearly