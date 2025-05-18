# System Patterns

This file documents recurring patterns and standards used in the project.
2025-03-29 23:11:09 - Initial file creation.

## Documentation Organization Pattern [2025-04-12 00:10]
* All project analysis, planning, and architecture documents must be stored in memory-bank/
* Key document types:
  - Analysis documents (e.g., auth-separation-analysis.md)
  - Planning documents (e.g., multi-tenancy-plan.md)
  - Architecture decisions (captured in decisionLog.md)
  - System patterns (captured in systemPatterns.md)
  - Active context (captured in activeContext.md)
* Benefits:
  - Centralized documentation
  - Consistent location for all project knowledge
  - Easier tracking of project evolution
  - Simplified reference management
[2025-05-18 17:04:23] - Documentation Maintenance Pattern:
* Remove implementation plan files once features are fully implemented
* Preserve key decisions and patterns in Memory Bank core files
* Update relevant sections in remaining documentation
* Benefits:
  - Reduced documentation overhead
  - Clearer active development focus
  - Maintained historical context in core files
  - Simplified documentation structure

## Coding Patterns

### API Endpoints
* RESTful design principles
* Minimal API routing patterns
* Consistent endpoint naming conventions
* Standard HTTP methods usage

### Data Access
* MongoDB repository pattern implementation
* Asynchronous database operations
* Structured error handling
* Data validation practices
* Document-based data modeling
* Query optimization patterns:
  - Use proper MongoDB indexing
  - Leverage MongoDB projection for field selection
  - Implement efficient MongoDB filters
  - Use MongoDB aggregation pipelines for complex queries
* Document relationship best practices:
  - Strategic use of embedded documents
  - References for large or frequently changing data
  - Denormalization for read performance
  - Proper index design for relationships
* MongoDB specific patterns:
  - Atomic operations for consistency
  - Bulk write operations for performance
  - Change streams for real-time updates
  - Proper connection and transaction handling

[2025-05-01 23:21:37] - Enhanced MongoDB data access patterns:
* Improved type handling in repository layer
* Standardized unit test patterns for data access
* Consistent error handling across repositories
* Enhanced query execution patterns
* Structured repository method naming conventions
[2025-04-14 20:20] - Updated data access patterns for MongoDB

[2025-04-03 20:22:44] - Added query optimization and entity relationship patterns

### File Handling
* Stream-based file operations
* Secure file storage practices
* File type validation
* Chunked upload support

### Logging Patterns
* Structured logging with consistent levels (Trace, Debug, Info, Warning, Error, Critical)
* Service-level logging for API operations and business logic
* Component-level logging for lifecycle and user interactions
* Centralized error handling and logging
* Performance monitoring through timing logs

[2025-03-30 17:37:29] - Established client-side logging patterns

## Architectural Patterns

### API Design
* Minimal API architecture
* Dependency injection
* Service-based architecture
* Repository pattern for data access

### UI Framework
* Pure Bootstrap implementation
* No custom CSS
* Built-in Bootstrap utilities for layout and spacing
* Bootstrap components for consistent UI patterns
* Bootstrap's responsive design system
* Bootstrap's built-in theming capabilities

### Security
* Token-based authentication (implemented)
* Role-based authorization (implemented)
* Secure file handling
* Input validation

### Data Flow
* Controller → Service → Repository pattern
* Async/await pattern for I/O operations
* Exception middleware for error handling
* Logging middleware for monitoring

## Testing Patterns

### Unit Testing
* Test method naming: Should_ExpectedBehavior_When_StateUnderTest
* Arrange-Act-Assert pattern
* Mock dependencies
* Test data builders

### Integration Testing
* Database testing with in-memory provider
* API endpoint testing
* File operation testing
* Authentication testing (planned)

## Development Environment Patterns

### Command Line Interface
* PowerShell as the standard shell for running commands
* Consistent command execution across development team
* Standardized terminal usage in development workflow

[2025-03-30 15:53:26] - Established PowerShell as the standard shell for running commands

[2025-03-30 16:10:47] - Command Chaining Pattern: On Windows PowerShell, commands must be chained using semicolon (;) instead of && for command chaining. Example: `cd ./some/path ; npm install` instead of `cd ./some/path && npm install`. This affects all command generation across the project.