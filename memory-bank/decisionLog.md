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
