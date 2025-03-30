
## 2025-03-30: Frontend Architecture Update
- Introduced Blazor WebAssembly as frontend framework
- Implemented client-side rendering with .NET 8
- Established pattern of WebAssembly client communicating with minimal API backend
- Maintained separation of concerns between frontend and backend
- Leveraged .NET ecosystem for full-stack development
- Enabled rich, interactive client-side experiences

# System Patterns

This file documents recurring patterns and standards used in the project.
2025-03-29 23:11:09 - Initial file creation.

## Coding Patterns

### API Endpoints
* RESTful design principles
* Minimal API routing patterns
* Consistent endpoint naming conventions
* Standard HTTP methods usage

### Data Access
* Entity Framework Core repository pattern
* Asynchronous database operations
* Structured error handling
* Data validation practices

### File Handling
* Stream-based file operations
* Secure file storage practices
* File type validation
* Chunked upload support

## Architectural Patterns

### API Design
* Minimal API architecture
* Dependency injection
* Service-based architecture
* Repository pattern for data access

### Security
* Token-based authentication (planned)
* Role-based authorization (planned)
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


## Development Environment Patterns

### Command Line Interface
* PowerShell as the standard shell for running commands
* Consistent command execution across development team
* Standardized terminal usage in development workflow

[2025-03-30 15:53:26] - Established PowerShell as the standard shell for running commands
* Authentication testing (planned)

[2025-03-30 16:10:47] - Command Chaining Pattern: On Windows PowerShell, commands must be chained using semicolon (;) instead of && for command chaining. Example: `cd ./some/path ; npm install` instead of `cd ./some/path && npm install`. This affects all command generation across the project.