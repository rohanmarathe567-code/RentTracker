# Decision Log

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
