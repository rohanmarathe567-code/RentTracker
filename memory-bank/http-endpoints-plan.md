# HTTP Endpoints Update Plan

## Overview
Update the RentTracker.http file to include comprehensive sample requests for all endpoints, organized by resource type.

## Base Structure
```
@baseUrl = http://localhost:5149
```

## Properties Endpoints

### GET Requests
1. Get all properties
```http
GET {{baseUrl}}/api/properties
Accept: application/json
```

2. Get property by ID
```http
GET {{baseUrl}}/api/properties/1
Accept: application/json
```

### POST Request
```http
POST {{baseUrl}}/api/properties
Content-Type: application/json

{
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
PUT {{baseUrl}}/api/properties/1
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
DELETE {{baseUrl}}/api/properties/1
```

## Payments Endpoints

### GET Requests
1. Get payments for property
```http
GET {{baseUrl}}/api/properties/1/payments
Accept: application/json
```

2. Get payment by ID
```http
GET {{baseUrl}}/api/payments/1
Accept: application/json
```

### POST Request
```http
POST {{baseUrl}}/api/payments
Content-Type: application/json

{
    "rentalPropertyId": 1,
    "amount": 1100.00,
    "paymentDate": "2024-03-30T00:00:00Z",
    "paymentMethod": "Bank Transfer",
    "paymentReference": "RENT-123456",
    "notes": "Rent payment for April 2024"
}
```

### PUT Request
```http
PUT {{baseUrl}}/api/payments/1
Content-Type: application/json

{
    "amount": 1100.00,
    "paymentDate": "2024-03-30T00:00:00Z",
    "paymentMethod": "Bank Transfer",
    "paymentReference": "RENT-123456-UPDATE",
    "notes": "Updated rent payment for April 2024"
}
```

### DELETE Request
```http
DELETE {{baseUrl}}/api/payments/1
```

## Attachments Endpoints

### GET Requests
1. Get attachment by ID
```http
GET {{baseUrl}}/api/attachments/1
Accept: application/json
```

2. Get property attachments
```http
GET {{baseUrl}}/api/properties/1/attachments
Accept: application/json
```

3. Get payment attachments
```http
GET {{baseUrl}}/api/payments/1/attachments
Accept: application/json
```

4. Download attachment
```http
GET {{baseUrl}}/api/attachments/1/download
```

### POST Requests
Note: These requests require multipart/form-data which needs to be tested through a client like Postman or using curl.

1. Upload property attachment:
```bash
curl -X POST {{baseUrl}}/api/properties/1/attachments \
  -F "file=@/path/to/file.pdf" \
  -F "description=Sample lease document"
```

2. Upload payment attachment:
```bash
curl -X POST {{baseUrl}}/api/payments/1/attachments \
  -F "file=@/path/to/receipt.pdf" \
  -F "description=Rent receipt"
```

### DELETE Request
```http
DELETE {{baseUrl}}/api/attachments/1