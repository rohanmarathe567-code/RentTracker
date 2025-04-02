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
