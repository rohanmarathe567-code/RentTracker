# Decision Log

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
