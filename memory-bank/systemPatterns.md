
## HTTP Endpoint Design Pattern [3/31/2025]
- RESTful API design
- GUID-based resource identification
- Consistent CRUD operation structure
- Multipart file upload support
- UTC timestamp standardization
- Nested resource relationships (e.g., payments and attachments under properties)
- Explicit error handling and validation


## [2025-03-31 19:45] - Pagination Pattern Implementation

### Infrastructure Components

1. Core Pagination Types:
   ```csharp
   // Models/Pagination/PaginationParameters.cs
   public class PaginationParameters
   - Base pagination request parameters
   - Configurable page size limits
   - Input validation

   // Models/Pagination/PaginatedResponse.cs
   public class PaginatedResponse<T>
   - Generic response wrapper
   - Metadata about pagination state
   - Collection of items

   // Extensions/PaginationExtensions.cs
   public static class PaginationExtensions
   - Extension methods for IQueryable<T>
   - Standardized pagination logic
   - Async support
   ```

### Usage Pattern
```csharp
// Controller usage
var result = await dbSet.Query
    .AsNoTracking()
    .ToPaginatedListAsync(parameters);
```

### Benefits
- Consistent pagination across all endpoints
- Reduced code duplication
- Centralized pagination configuration
- Easy to maintain and modify

---


## Sequential GUID Generation Pattern
### Overview
- Implemented a custom GUID generation strategy that ensures:
  1. Globally unique identifiers
  2. Sortability across entities
  3. Randomness to prevent predictability

### Implementation Details
- SequentialGuidGenerator uses:
  * Timestamp-based generation
  * Cryptographically secure random bytes
  * Reversing timestamp bytes for proper sorting

### Affected Models
- RentalProperty
- RentalPayment
- Attachment

### Benefits
- Supports distributed systems
- Maintains data integrity
- Enables chronological sorting
- Provides unique identifiers across all entities

## [2025-03-30] Nested Resource Endpoint Pattern for Payments

### Context
Implemented a nested resource endpoint pattern for payments within properties, following RESTful API design principles.

### Pattern Description
- Endpoint Structure: `/api/properties/{propertyId}/payments/{paymentId}`
- Supports operations: GET, PUT, DELETE
- Provides context-specific access to payment resources

### Key Characteristics
- Hierarchical resource representation
- Explicit property-payment relationship
- Enhanced security through context-based validation

### Implementation Details
- Validates payment belongs to specified property before allowing operations
- Maintains existing service layer methods
- Provides intuitive API navigation

### Benefits
- Improved API readability
- Stronger data integrity
- More precise resource management

### Example
```
GET /api/properties/123/payments/456
- Retrieves payment 456 specifically for property 123
```

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