[2025-04-12 22:22] - Fixed property update issues with version handling

- Changed: Modified property update logic to properly handle optimistic concurrency
- Rationale: Updates were failing silently due to incorrect version handling
- Changes made:
  * Now using database version instead of client-provided version
  * Properly preserving CreatedAt timestamp
  * Correctly setting UpdatedAt for current update
- Impact: All property fields (including PropertyManagerName and Description) will now update correctly

[2025-04-12 22:17] - Added PropertyManagerName and PropertyManagerContact fields to RentalProperty

- Changed: Moved property manager information from Attributes dictionary to dedicated model fields
- Rationale: Improve data structure and ensure consistent storage of property manager information
- Changes made:
  * Added fields to RentalProperty model
  * Updated property creation and update endpoints
  * Modified search functionality to use new fields
- Impact: Provides more reliable storage and retrieval of property manager information

# Decision Log

This file records architectural and implementation decisions using a list format.

## 2025-04-12 09:16 - Database Technology Migration to MongoDB

### Decision
Migrate from PostgreSQL to MongoDB as the primary database for RentTracker application.

### Rationale
- Product is still in development with no production data
- Schema flexibility needed for evolving property attributes
- Document-based structure aligns well with property-centric data model
- No complex migration needed as system is not live
- Better support for future extensibility

### Implementation Details
1. **Data Model Design**
   - Property as root document
   - Embedded arrays for payments and attachments
   - Flexible schema for future attributes
   - Multi-tenant support through document structure

2. **Technical Architecture**
   - MongoDB.Driver for .NET
   - Repository pattern adaptation
   - Integration with existing Redis cache
   - Maintain current API contracts

3. **Database Structure**
```json
{
  "property": {
    "_id": "ObjectId",
    "tenantId": "string",
    "address": {
      "street": "string",
      "city": "string",
      "state": "string",
      "zipCode": "string"
    },
    "rentAmount": "decimal",
    "leaseDates": {
      "startDate": "date",
      "endDate": "date"
    },
    "payments": [{
      "_id": "ObjectId",
      "amount": "decimal",
      "date": "date",
      "type": "string",
      "notes": "string"
    }],
    "attachments": [{
      "_id": "ObjectId",
      "fileName": "string",
      "contentType": "string",
      "uploadDate": "date",
      "path": "string"
    }],
    "attributes": {
      // Flexible key-value pairs for future extensions
    }
  }
}
```

### Key Benefits
- Schema flexibility for future attributes
- Better representation of hierarchical data
- Simplified queries for property-centric operations
- Native support for JSON-like data structures
- Easier addition of new features

### Migration Steps
1. Set up MongoDB development environment
2. Create new data access layer
3. Implement repository pattern for MongoDB
4. Update API endpoints to use new data layer
5. Migrate existing development data
6. Update tests for MongoDB operations

### Performance Considerations
- Index design for common queries
- Compound indexes for multi-tenant queries
- Denormalization strategies for frequent reads
- Monitoring and optimization guidelines

## 2025-04-10 22:28 - Multi-Tenancy Architecture Design

[Previous entries remain unchanged...]
