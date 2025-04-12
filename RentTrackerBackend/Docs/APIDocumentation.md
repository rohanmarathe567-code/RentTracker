# RentTracker API Documentation

## Authentication
All endpoints require authentication using JWT Bearer token.

## Properties API

### List Properties
```http
GET /api/properties
```

#### Query Parameters
- `pageNumber` (int): Page number for pagination (default: 1)
- `pageSize` (int): Number of items per page (default: 10)
- `searchTerm` (string): Optional search term for filtering properties
- `sortField` (string): Field to sort by (address, suburb, state, weeklyrentamount, leasestartdate, leaseenddate)
- `sortDescending` (boolean): Sort direction (default: false)

#### Response
```json
{
  "items": [
    {
      "id": "string",
      "tenantId": "string",
      "address": {
        "street": "string",
        "city": "string",
        "state": "string",
        "zipCode": "string"
      },
      "rentAmount": 0,
      "leaseDates": {
        "startDate": "string",
        "endDate": "string"
      },
      "createdAt": "string",
      "updatedAt": "string",
      "version": 0
    }
  ],
  "totalCount": 0,
  "pageNumber": 0,
  "totalPages": 0,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### Get Property by ID
```http
GET /api/properties/{id}
```

#### Parameters
- `id` (string): MongoDB ObjectId of the property

#### Response
```json
{
  "id": "string",
  "tenantId": "string",
  "address": {
    "street": "string",
    "city": "string",
    "state": "string",
    "zipCode": "string"
  },
  "rentAmount": 0,
  "leaseDates": {
    "startDate": "string",
    "endDate": "string"
  },
  "createdAt": "string",
  "updatedAt": "string",
  "version": 0
}
```

### Search Properties
```http
GET /api/properties/search
```

#### Query Parameters
- `searchText` (string): Text to search for in address fields
- `pageNumber` (int): Page number for pagination
- `pageSize` (int): Items per page

#### Notes
- Uses MongoDB text search on address fields
- Returns paginated results
- Results are sorted by relevance

### Create Property
```http
POST /api/properties
```

#### Request Body
```json
{
  "address": {
    "street": "string",
    "city": "string",
    "state": "string",
    "zipCode": "string"
  },
  "rentAmount": 0,
  "leaseDates": {
    "startDate": "string",
    "endDate": "string"
  }
}
```

#### Notes
- TenantId is automatically set from authenticated user
- CreatedAt, UpdatedAt, and Version are managed by the system
- Returns 201 Created with the new property data

### Update Property
```http
PUT /api/properties/{id}
```

#### Parameters
- `id` (string): MongoDB ObjectId of the property

#### Request Body
Same as Create Property

#### Notes
- Version field is used for optimistic concurrency
- Returns 409 Conflict if version mismatch
- Returns 404 if property not found

### Delete Property
```http
DELETE /api/properties/{id}
```

#### Parameters
- `id` (string): MongoDB ObjectId of the property

#### Notes
- Returns 204 No Content on success
- Returns 404 if property not found

## Error Responses

### 400 Bad Request
```json
{
  "message": "Validation error message"
}
```

### 401 Unauthorized
Returned when:
- No authentication token provided
- Invalid authentication token

### 404 Not Found
Returned when requested resource doesn't exist

### 409 Conflict
```json
{
  "message": "Concurrency conflict - the document has been modified by another user"
}
```

### 500 Internal Server Error
```json
{
  "title": "Error title",
  "detail": "Error details",
  "status": 500
}
```

## MongoDB-Specific Features

### Indexing
The API utilizes the following indexes for optimal performance:
- Compound index on (TenantId, RentAmount)
- Text index on Address fields for search
- Index on Version field for concurrency control
- Date-based indexes for lease queries

### Caching
- Properties are cached in Redis for 30 minutes
- Cache is automatically invalidated on updates
- Cache key format: `{tenantId}:property:{propertyId}`

### Pagination
- All list operations are paginated
- Default page size: 10
- Maximum page size: 100
- Sorted by UpdatedAt by default

### Search
- Full-text search implemented using MongoDB text indexes
- Searches across all address fields
- Results sorted by text score relevance