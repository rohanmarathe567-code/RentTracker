[2025-04-26 19:30:00] - Backend Service Layer Enhancement

## Decision
Implement dedicated service layer and improve dependency injection across the backend.

## Rationale
1. Previous implementation:
   - Business logic mixed in controllers
   - Repeated authorization logic
   - Direct repository usage in controllers
   - Tight coupling between components

2. New implementation:
   - Dedicated service layer for business logic
   - Centralized claims handling via ClaimsPrincipalService
   - Improved separation of concerns
   - Better testability and maintainability

## Implementation Details
1. Added ClaimsPrincipalService:
   - Centralized tenant ID validation
   - Unified role checking
   - Consistent claims access

2. Added PropertyService:
   - Encapsulated property-related business logic
   - Implemented proper pagination and sorting
   - Centralized property validation
   - Improved error handling

3. Updated Controllers:
   - Removed business logic
   - Use dependency injection for services
   - Simplified endpoint implementations
   - Better error handling

## Impact
- Improved code organization
- Better separation of concerns
- Enhanced testability
- Reduced code duplication
- More maintainable codebase
- Clearer responsibility boundaries
# Decision Log

[2025-04-14 20:22:00] - MongoDB Migration Implementation

## Decision
Migrate from PostgreSQL to MongoDB for better schema flexibility and document-based storage.

## Rationale
1. Previous PostgreSQL implementation:
   - Rigid schema structure
   - Complex migrations for schema changes
   - Limited support for flexible attributes
   - Performance overhead for nested data

2. MongoDB advantages:
   - Flexible schema design
   - Native support for document-based data
   - Better performance for nested/hierarchical data
   - Simplified property customization
   - Improved scalability
   - Native support for JSON-like data structures

## Implementation Details
1. Data Model Changes:
   - Converted Entity Framework models to MongoDB documents
   - Implemented embedded documents for related data
   - Designed proper MongoDB indices
   - Updated repository pattern for MongoDB

2. API Enhancements:
   - Leveraged MongoDB query capabilities
   - Implemented MongoDB-specific optimizations
   - Added support for flexible property attributes
   - Enhanced include functionality

3. Performance Considerations:
   - Strategic indexing for common queries
   - Proper connection handling
   - Efficient document design
   - Optimized query patterns

## Migration Impact
- Improved schema flexibility
- Better performance for nested data
- Simplified property customization
- Enhanced scalability
- Reduced complexity for schema changes

[2025-04-13 21:47:00] - PaymentMethod Include Implementation

## Decision
Implement PaymentMethod includes at the backend instead of frontend data mapping.

## Rationale
1. Frontend solution (current):
   - Requires additional network request
   - More client-side processing
   - Data consistency relies on frontend logic
   - Not scalable if payment methods grow

2. Backend solution (proposed):
   - Single network request for complete data
   - Better data consistency (handled at data layer)
   - More efficient (no client-side processing)
   - Follows proper REST principles
   - Better performance with large datasets

## Implementation Details
1. Modify PaymentRepository:
   ```csharp
   public class PaymentRepository : MongoRepository<RentalPayment>
   {
       private readonly IMongoCollection<PaymentMethod> _paymentMethodCollection;
       
       // Add PaymentMethod lookup capabilities
       protected override async Task<IEnumerable<RentalPayment>> GetAllWithIncludesAsync(string tenantId, string[]? includes)
       {
           var payments = await base.GetAllAsync(tenantId);
           
           if (includes?.Contains("PaymentMethod") == true)
           {
               foreach (var payment in payments)
               {
                   if (!string.IsNullOrEmpty(payment.PaymentMethodId))
                   {
                       payment.PaymentMethod = await _paymentMethodCollection
                           .Find(x => x.Id == payment.PaymentMethodId)
                           .FirstOrDefaultAsync();
                   }
               }
           }
           
           return payments;
       }
   }
   ```

2. Update PaymentService:
   - Pass includes parameter through service layer
   - Handle includes in repository calls

3. Update API Endpoints:
   - Parse and validate include parameters
   - Pass includes to service layer

This approach:
- Maintains proper separation of concerns
- Handles data relationships at the data layer
- Follows REST principles for related data
- Is more performant and scalable

[2025-04-14 21:33:13] - TenantId Implementation Change

## Decision
Change TenantId from using email to using MongoDB ObjectId.

## Rationale
1. Previous implementation (email-based):
   - Easily guessable tenant IDs
   - Potential security risk
   - Exposed user information in system identifiers

2. New implementation (ObjectId-based):
   - More secure (non-guessable IDs)
   - Follows MongoDB best practices
   - Maintains data privacy
   - Consistent with database ID usage

## Implementation Details
1. Modified AuthService:
   - Changed TenantId assignment to use Id.ToString() instead of email
   - Updated JWT claims to include both Id and email
   - Added explicit userId claim for better clarity

2. Updated DatabaseSeeder:
   - Changed system user creation to use ObjectId for TenantId
   - Updated all dependent entities to use the new TenantId format

## Impact
- Improved security through non-guessable tenant IDs
- Better alignment with MongoDB patterns
- No functional changes to existing relationships
- Maintains backward compatibility with string-based TenantId

[2025-04-15 00:12:31] - Property Attributes Refactoring

## Decision
Move PropertyManager and Description from Attributes dictionary to dedicated model properties.

## Rationale
1. Previous implementation:
   - Stored PropertyManager and Description in generic Attributes dictionary
   - Reduced type safety and validation
   - Inconsistent with object-oriented design
   - Harder to maintain and query

2. New implementation:
   - Dedicated PropertyManager class with Name and Contact properties
   - Description as a first-class property
   - Better type safety and validation
   - Improved code maintainability and readability

## Implementation Details
1. Updated RentalProperty model:
   - Added Description property
   - Added PropertyManager class with Name and Contact properties
   - Maintains Attributes dictionary for truly dynamic properties

2. Updated DatabaseSeeder:
   - Removed PropertyManager and Description from Attributes dictionary
   - Uses proper model properties instead

## Impact
- Improved type safety and validation
- Better code maintainability
- Clearer data structure
- More consistent with object-oriented principles
