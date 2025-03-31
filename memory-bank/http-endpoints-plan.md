# HTTP Endpoints Update Plan

## Overview
Comprehensive HTTP endpoints for RentTracker, covering properties, payments, and attachments with detailed request/response specifications.

## Base Structure
```
@baseUrl = http://localhost:7000
```

## Health Check Endpoint
### GET Request
```http
GET {{baseUrl}}/api/health
Accept: application/json
```
- Returns system health status
- Checks database connectivity

## Properties Endpoints

### GET Requests
1. Get all properties
```http
GET {{baseUrl}}/api/properties
Accept: application/json
```

2. Get property by ID
```http
GET {{baseUrl}}/api/properties/{propertyId:guid}
Accept: application/json
```

### POST Request
```http
POST {{baseUrl}}/api/properties
Content-Type: application/json

{
    "id": "00000000-0000-0000-0000-000000000000", // Optional: Server may generate
    "address": "123 Sample Street",
    "suburb": "Sample Suburb",
    "state": "NSW",
    "postCode": "2000",
    "description": "Modern 2-bedroom apartment",
    "weeklyRentAmount": 550.00,
    "leaseStartDate": "2024-01-01T00:00:00Z",
    "leaseEndDate": "2024-12-31T00:00:00Z",
    "propertyManager": "John Smith",
    "propertyManagerContact": "john.smith@example.com"
}
```

### PUT Request
```http
PUT {{baseUrl}}/api/properties/{propertyId:guid}
Content-Type: application/json

{
    "address": "123 Updated Street",
    "suburb": "Updated Suburb",
    "state": "NSW",
    "postCode": "2000",
    "description": "Updated description",
    "weeklyRentAmount": 575.00,
    "leaseStartDate": "2024-01-01T00:00:00Z",
    "leaseEndDate": "2024-12-31T00:00:00Z",
    "propertyManager": "Jane Doe",
    "propertyManagerContact": "jane.doe@example.com"
}
```

### DELETE Request
```http
DELETE {{baseUrl}}/api/properties/{propertyId:guid}
```

## Payments Endpoints

### GET Requests
1. Get payments for a specific property
```http
GET {{baseUrl}}/api/properties/{propertyId:guid}/payments
Accept: application/json
```

2. Get a specific payment by ID for a property
```http
GET {{baseUrl}}/api/properties/{propertyId:guid}/payments/{paymentId:guid}
Accept: application/json
```

### POST Request
```http
POST {{baseUrl}}/api/properties/{propertyId:guid}/payments
Content-Type: application/json

{
    "id": "00000000-0000-0000-0000-000000000000", // Optional: Server may generate
    "rentalPropertyId": "{propertyId:guid}", // Explicit GUID reference
    "amount": 1100.00,
    "paymentDate": "2024-03-30T00:00:00Z",
    "paymentMethod": "Bank Transfer",
    "paymentReference": "RENT-123456",
    "notes": "Rent payment for April 2024"
}
```

### PUT Request
```http
PUT {{baseUrl}}/api/properties/{propertyId:guid}/payments/{paymentId:guid}
Content-Type: application/json

{
    "amount": 1200.00,
    "paymentDate": "2024-03-30T00:00:00Z",
    "paymentMethod": "Credit Card",
    "paymentReference": "RENT-123456-UPDATE",
    "notes": "Updated rent payment for April 2024"
}
```

### DELETE Request
```http
DELETE {{baseUrl}}/api/properties/{propertyId:guid}/payments/{paymentId:guid}
```

## Attachments Endpoints

### GET Requests
1. Get property attachments
```http
GET {{baseUrl}}/api/properties/{propertyId:guid}/attachments
Accept: application/json
```

2. Get payment attachments for a specific property
```http
GET {{baseUrl}}/api/properties/{propertyId:guid}/payments/{paymentId:guid}/attachments
Accept: application/json
```

3. Get attachment by ID
```http
GET {{baseUrl}}/api/attachments/{attachmentId:guid}
Accept: application/json
```

### POST Requests
Note: These requests require multipart/form-data which needs to be tested through a client like Postman or using curl.

1. Upload property attachment:
```bash
curl -X POST {{baseUrl}}/api/properties/{propertyId:guid}/attachments \
  -F "file=@/path/to/file.pdf" \
  -F "description=Sample lease document" \
  -F "id=00000000-0000-0000-0000-000000000000" # Optional: Server may generate
```

2. Upload payment attachment:
```bash
curl -X POST {{baseUrl}}/api/properties/{propertyId:guid}/payments/{paymentId:guid}/attachments \
  -F "file=@/path/to/receipt.pdf" \
  -F "description=Rent receipt" \
  -F "id=00000000-0000-0000-0000-000000000000" # Optional: Server may generate
```

### DELETE Request
```http
DELETE {{baseUrl}}/api/attachments/{attachmentId:guid}
```

## Notes
- All timestamps are in UTC
- Ensure proper authentication and authorization for all endpoints
- Error handling is implemented for various scenarios
- All IDs are now GUIDs (Globally Unique Identifiers)
- Optional ID field can be provided, but server may generate its own sequential GUID