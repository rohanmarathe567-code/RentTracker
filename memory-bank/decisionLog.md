# Decision Log

## Theme Implementation Strategy
**Date:** 2025-04-05

### Decision
Implement application theming with the following approach:
1. Define a consistent theme system using CSS variables
2. Store theme preferences in localStorage initially
3. Plan for future migration to database storage alongside view preferences

### Rationale
- Need for consistent visual experience
- Alignment with existing view preference storage strategy
- Future extensibility for user customization
- Maintainable approach using CSS variables

### Implementation Details
#### Phase 1 (Current)
- Store theme preference in localStorage as "appTheme"
- Implement theme switching mechanism in shared layout
- Use CSS variables for colors, spacing, and typography
- Default to light theme if no preference is set

#### Phase 2 (Future)
- Migrate to database storage alongside other user preferences
- Expand theme customization options
- Support per-user theme settings

### Technical Impact
- Improved visual consistency
- Enhanced maintainability through CSS variables
- Minimal database impact in Phase 1
- Aligned with existing preference storage patterns

## View Preference Storage Strategy
**Date:** 2025-04-03

### Decision
Implement view preference (card/table view) storage in two phases:
1. **Phase 1 (Current):** Use browser's localStorage
2. **Phase 2 (Future):** Migrate to database storage when implementing authentication

### Rationale
- Immediate need: Maintain view preference across app executions
- Current state: No authentication/user system yet
- Future plans: Multi-tenancy with authentication is planned
- Complexity: Avoid premature database schema changes
- Migration path: Easy to migrate from localStorage to database when needed

### Implementation Details
#### Phase 1 (localStorage)
- Store preference as "propertyListViewMode" with values "card"/"table"
- Implement in PropertyList.razor using browser's localStorage
- Default to "table" view if no preference is set

#### Phase 2 (Future Database)
- Will be implemented alongside authentication
- Migration strategy will be developed to preserve user preferences
- Consider implementing a generic UserPreferences table for extensibility

### Technical Impact
- Minimal database impact in Phase 1
- Frontend-only changes required initially
- Clean separation of concerns
- Future-proofed design approach

This file records architectural and implementation decisions using a list format.
2024-04-02 19:34:38 - Log of updates made.

## Decision: Backend-Only ID Generation

### Context
- Currently, the frontend has some control over property ID generation
- The frontend sets property.Id = Guid.Empty for new properties
- The backend only generates new IDs when it receives Guid.Empty

### Decision
Move all ID generation responsibility to the backend to ensure proper separation of concerns and consistent ID generation.

### Rationale
1. Security: Frontend should not have any control over ID generation
2. Consistency: Backend should be the single source of truth for ID generation
3. Separation of Concerns: ID generation is a backend responsibility

### Implementation Details
1. Frontend Changes (PropertyEdit.razor):
   - Remove property.Id = Guid.Empty assignment
   - Remove any other ID handling logic
   - Let backend handle all ID generation

2. Backend Changes (PropertiesController.cs):
   - Always generate new IDs for new properties using SequentialGuidGenerator
   - Ignore any ID values sent from frontend for new properties
   - Maintain existing ID validation for updates

### Impact
- Improved security through proper separation of concerns
- More consistent ID generation
- Cleaner frontend code with less responsibility

## Decision: Property to Payments Navigation

### Context
- Currently, clicking on a property row in the property list navigates to the property edit page
- There's no direct way to view payments for a specific property from the property list
- Users need to easily access payment information for properties

### Decision
Implement navigation from the property list to the payments list when a property row is clicked, and add a dedicated Payments button in the actions column.

### Rationale
1. Usability: Users frequently need to check payments for properties, making this a common workflow
2. Consistency: Maintain the same UI patterns (pagination, styling, edit/delete buttons) across the application
3. Efficiency: Reduce the number of clicks needed to access payment information

### Implementation Details
1. PropertyList.razor Changes:
   - Modified row click handler to navigate to payments page instead of edit page
   - Added a dedicated Payments button in the actions column
   - Kept the Edit button functionality as is

2. Payments.razor Enhancements:
   - Added pagination support matching the property list implementation
   - Improved UI to match the property list styling
   - Added property-specific context when viewing payments for a specific property
   - Implemented search functionality for payments
   - Added confirmation modal for payment deletion
   - Added navigation back to properties list

### Impact
- Improved user experience through more intuitive navigation
- Enhanced consistency across the application
- More efficient workflow for managing property payments

## Decision: Implement POST Endpoint for Payments

### Context
- The system was missing the POST endpoint for /api/properties/{propertyId}/payments
- The IPaymentService interface lacked a CreatePaymentAsync method
- The endpoint was already documented in the HTTP endpoints plan

### Decision
Implement the missing POST endpoint for creating payments associated with a specific property.

### Rationale
1. Completeness: The API needed full CRUD operations for payments
2. Consistency: Follow the same patterns used in other endpoints
3. Usability: Allow users to create new payments through the API

### Implementation Details
1. Service Layer Changes:
   - Added CreatePaymentAsync method to IPaymentService interface
   - Implemented method in PaymentService with property validation
   - Always generate new IDs for payments, ignoring any client-provided IDs
   - Ensured proper timestamp handling

2. Controller Changes:
   - Added POST endpoint to PaymentsController
   - Implemented proper error handling and validation
   - Returns 201 Created with location header pointing to the new resource

### Impact
- Complete CRUD operations for payments
- Consistent API design across the application
- Improved developer experience with predictable patterns
- Updated README.md API documentation to reflect current endpoints

[2025-04-03 19:18:11]

## Decision: Update Client Code to Use Nested Payment Endpoint

### Context
- The backend has implemented a POST endpoint for /api/properties/{propertyId}/payments
- The client code was still using the old endpoint structure for creating payments
- The client needed to be updated to use the new nested endpoint structure

### Decision
Update the RentalPaymentService.CreatePaymentAsync method to use the new nested endpoint structure.

### Rationale
1. Consistency: Align client code with the backend API structure
2. Proper Resource Hierarchy: Follow RESTful design principles with nested resources
3. Improved Data Integrity: Ensure payments are properly associated with properties

### Implementation Details
1. RentalPaymentService.cs Changes:
   - Modified CreatePaymentAsync to use "../properties/{payment.RentalPropertyId}/payments" endpoint
   - Used the relative path pattern to navigate from the base URL

2. Payments.razor Changes:
   - Added explicit check to ensure RentalPropertyId is set correctly
   - Improved error logging

### Impact
- Improved API consistency between client and server
- Better adherence to RESTful design principles
- Enhanced data integrity with explicit property-payment relationship

[2025-04-03 19:49:35]

## Decision: Optimize Payment Query Loading

### Context
- Payment endpoints were potentially eager loading RentalProperty data
- The RentalProperty navigation property exists but isn't used in API responses
- Performance could be impacted by unnecessary data loading

### Decision
Optimize payment queries by implementing explicit lazy loading and removing unnecessary property loading.

### Rationale
1. Performance: Reduce unnecessary data fetching from database
2. Efficiency: Minimize memory usage and network bandwidth
3. Best Practice: Follow the principle of loading only required data

### Implementation Details
1. RentalPayment Model Changes:
   - Update navigation property to be explicitly lazy loaded
   - Maintain proper foreign key relationship

2. PaymentService Changes:
   - Modify queries to use AsNoTracking()
   - Ensure RentalProperty is not eagerly loaded
   - Optimize IQueryable implementations

### Impact
- Improved query performance
- Reduced memory usage
- Lower network bandwidth consumption
- No change to API contract or responses

[2025-04-03 20:21:11]
