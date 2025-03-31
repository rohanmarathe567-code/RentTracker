
[2025-03-31 20:14:39] - Created k6 performance testing scripts for RentTrackerBackend
- Added performance test scripts for Health, Properties, Payments, and Attachments endpoints
- Utilized Faker.js for generating realistic test data
- Implemented various load testing scenarios with different virtual user configurations
- Added README.md with setup and running instructions

## HTTP Endpoints Design Decisions [3/31/2025]
- Adopted GUID for all resource identifiers
- Implemented nested resource model (properties > payments > attachments)
- Chose multipart/form-data for file uploads
- Standardized on UTC timestamps
- Base URL set to http://localhost:7000 for development
- Implemented comprehensive error handling strategy
- Designed flexible POST/PUT endpoints allowing optional server-generated IDs


## [2025-03-31 19:44] - Property Endpoint Optimization

### Decision
Optimize the GET /api/properties endpoint by:
1. Removing eager loading of payments and attachments
2. Adding pagination support
3. Using AsNoTracking for better performance

### Rationale
- Current implementation returns all payments and attachments with properties, causing large response sizes
- Separate endpoints already exist for fetching payments (/api/properties/{propertyId}/payments)
- Pagination is needed to handle large datasets efficiently
- AsNoTracking improves performance for read-only operations

### Implementation Details
1. Properties endpoint changes:
   - Remove navigation property loading
   - Add pagination parameters (page number, page size)
   - Add AsNoTracking for better performance
2. Continue using existing payments endpoint for payment data
3. Update documentation to reflect these changes

### Impact
- Improved API performance
- Reduced response sizes
- Better scalability for large datasets
- Clearer separation of concerns between properties and payments

---


## [2025-03-30] HTTP Endpoint GUID Updates
- Updated RentTracker.http to use GUID-based identifiers
- Added example GUIDs for properties, payments, and attachments
- Maintained existing endpoint structure
- Introduced variables for easier testing and demonstration

Rationale:
- Consistent with new GUID-based model implementation
- Provides clear examples of GUID usage in API interactions
- Supports testing and documentation of new ID format


## [2025-03-30] Sequential GUID Implementation
- Replaced integer-based primary keys with sequential GUIDs in RentalProperty, RentalPayment, and Attachment models
- Created SequentialGuidGenerator to ensure sortable and random GUIDs
- Updated foreign key relationships to use Guid type
- Maintained existing navigation properties and relationships

Rationale:
- Provides globally unique identifiers
- Enables sorting and tracking of entities
- Maintains randomness to prevent predictability
- Supports distributed systems and data integrity

## [2025-03-30] HTTP Endpoint Structure Refinement
- Decision: Modify endpoint structure to include more explicit property context
- Rationale: Improve API clarity and enforce tighter relationship between payments/attachments and their parent properties
- Implications:
  * Payments and attachments now require propertyId in their URLs
  * More granular access control and validation
  * Improved data integrity and referential constraints
- Technical Changes:
  * Updated RentTracker.http test file
  * Updated http-endpoints-plan.md documentation
  * Refactored backend endpoint mappings

## [2025-03-30] Attachment Endpoint Modification

- Removed DELETE endpoint for /api/attachments/{attachmentId}
- Removed DeleteAttachmentAsync method from IAttachmentService
- Removed DeleteAttachmentAsync method from AttachmentService implementation

Rationale: DELETE functionality for attachments is no longer required. Simplified the attachment service and endpoint structure to remove unnecessary deletion capabilities.
## [2025-03-30] Payment Endpoint Expansion
- Added new endpoints for `/api/properties/{propertyId}/payments/{paymentId}`
- Implemented GET, PUT, and DELETE methods with property-specific validation
- Ensures payments can only be accessed, modified, or deleted in the context of their associated property
- Provides more granular and secure access to payment resources

### Rationale
- Improves API design by creating more RESTful and context-aware endpoints
- Adds an extra layer of security by validating property ownership for payment operations
- Allows for more precise management of payments within the context of specific properties

### Implications
- Clients can now perform property-specific payment operations more intuitively
- Requires updates to client-side API interaction logic
- Maintains existing payment service methods while adding new routing capabilities
## Attachment System Implementation (2025-03-30)

### Key Decisions
- Implemented comprehensive attachment handling for properties and payments
- Created AttachmentService with robust file validation and storage
- Implemented endpoints for:
  * Listing attachments for properties
  * Listing attachments for payments
  * Uploading attachments to properties
  * Uploading attachments to payments
  * Deleting attachments

### Technical Details
- File upload validation:
  * Supported file types: PDF, DOC, DOCX, JPG, JPEG, PNG, GIF, BMP, TIFF, TXT, CSV, XLS, XLSX
  * Maximum file size: 10MB
- Secure file storage with unique filename generation
- Separate storage directories for property and payment attachments

### Rationale
- Provides flexible attachment management for rental properties and payments
- Ensures file security through validation and controlled storage
- Supports multiple attachment types for comprehensive record-keeping

### Future Improvements
- Implement file preview functionality
- Add more granular file type restrictions if needed
- Consider implementing file compression for large attachments

Timestamp: 2025-03-30 19:33 AEDT

[2025-03-30 20:52:20] - Refactored attachment and payment services
- Updated multiple services, models, and endpoints related to attachments and payments
- Added SequentialGuidGenerator for improved GUID generation
- Committed changes to master branch
- Modifications span across Controllers, Services, and Models
