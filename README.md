# RentTracker

[![Project Status: Active](https://img.shields.io/badge/status-active-brightgreen.svg)](https://github.com/yourusername/RentTracker)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![.NET Build](https://github.com/yourusername/RentTracker/workflows/build/badge.svg)](https://github.com/yourusername/RentTracker/actions)

A comprehensive multi-tenant rental property management system for tracking payments and managing property-related documents.

## Overview

RentTracker is a modern property management solution built with ASP.NET Core and Blazor WebAssembly that helps landlords efficiently manage their rental properties, track payments, and handle property-related documents. The system provides an intuitive web application with a robust backend API for property management operations while maintaining secure data storage, multi-tenancy support, and file handling capabilities.

## Getting Started

### Quick Start

```bash
# Clone the repository
git clone https://github.com/yourusername/RentTracker.git
cd RentTracker

# Start MongoDB and Redis using Docker
docker-compose up -d

# Configure connection strings in appsettings.json
{
  "MongoDb": {
    "ConnectionString": "mongodb://root:example@localhost:27017",
    "DatabaseName": "RentTracker"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}

# Install dependencies and run backend
cd RentTrackerBackend
dotnet restore
dotnet run

# In another terminal, start the frontend
cd ../RentTrackerClient
dotnet run
```

### Development Requirements
- .NET 8 SDK
- Docker Desktop
- MongoDB Compass (optional, for database management)

### Initial Setup
1. MongoDB will automatically create required collections
2. Default indexes are created on startup
3. Redis cache is initialized with default configuration
4. Sample data can be loaded using the `/api/dev/seed` endpoint (development only)

## Features

### Existing Features
* Multi-tenancy Support
  - Advanced tenant isolation using MongoDB's built-in features
  - JWT-based authentication with tenant context
  - Role-based access control with Admin and Normal user types
  - Efficient tenant-specific data queries
* Document-based Property Management
  - Flexible schema design with embedded payments and attachments
  - Extensible property attributes through dynamic fields
  - Optimized MongoDB indexes for fast property lookups
  - Full-text search capabilities for property details
* Enhanced Payment Tracking
  - Embedded payment arrays for efficient retrieval
  - Atomic payment operations using MongoDB operators
  - Optimistic concurrency control for payment updates
  - Rich payment history with document versioning
* High-Performance Data Access
  - Redis caching layer for frequently accessed data
  - Intelligent cache invalidation strategies
  - Optimized MongoDB aggregation pipelines
  - Efficient pagination using cursor-based approach
* Document Management
  - GridFS integration for large file storage
  - Secure file handling with metadata tracking
  - Efficient binary data streaming
  - Document versioning and audit trails
* Scalable Architecture
  - Horizontally scalable MongoDB deployment
  - Distributed caching with Redis
  - Optimized compound indexes
  - Built-in data versioning and concurrency control

### RentTrackerClient Features
* Modern Component Architecture
  - Reactive components with real-time updates
  - Smart caching integration with MongoDB
  - Optimized data synchronization
  - Efficient state management
* Enhanced User Experience
  - Fast document-based data access
  - Real-time property updates
  - Intelligent cache utilization
  - Responsive data loading
* Service Layer
  - MongoDB-aware typed HTTP clients
  - Optimized document-based responses
  - Intelligent error handling
  - Efficient cache management
* Performance Optimizations
  - Smart MongoDB query optimization
  - Efficient document caching
  - Minimal network overhead
  - Optimized data structures

### Planned Features
* Docker Containerization with CI/CD Pipeline
* Enhanced Reporting
  - Financial analysis tools
  - Custom report generation
  - Data visualization
* Dashboard with Key Metrics
* Payment Reminder System
* Improved Data Export
* Advanced Search and Filtering
* Enhanced API Documentation

## Project Status and Roadmap
### Current Status
- [x] MongoDB Migration Complete
- [x] Redis Cache Integration
- [x] Enhanced Document-based Storage
- [x] Optimized MongoDB Indexing
- [x] GridFS Implementation
- [x] Multi-tenancy Enhancement
- [ ] Docker Containerization
- [ ] Advanced Reporting
- [ ] Payment Reminder System

### Recent Achievements
- ✅ Completed migration to MongoDB
- ✅ Implemented Redis caching layer
- ✅ Optimized MongoDB indexes and queries
- ✅ Enhanced data model with document-based design
- ✅ Implemented GridFS for file storage
- ✅ Improved performance with caching strategies
- ✅ Updated client architecture for multi-tenant support

### Upcoming Milestones
1. Q2 2025: Docker Containerization and CI/CD Pipeline
2. Q3 2025: Enhanced Reporting Features
3. Q4 2025: Advanced Analytics and Payment Reminders

## Architecture

[Additional architecture details available in docs/multi-tenancy-plan.md]

## Database Schema

The system uses MongoDB collections to implement a flexible, document-based data model for multi-tenant rental property management. Each document includes built-in versioning, tenant isolation, and audit fields:

### Base Document Structure
All documents inherit these base fields:
```json
{
    "_id": ObjectId,
    "tenantId": string,
    "createdAt": DateTime,
    "updatedAt": DateTime,
    "version": long
}
```

### Collections

1. **Users**:
   - Core collection for authentication and authorization
   - Supports multiple user types (Admin/Normal)
   - Controls access to properties and payments
   ```json
   {
       "email": string,
       "passwordHash": string,
       "userType": string,
       "attributes": { } // Flexible attributes
   }
   ```

2. **RentalProperties**:
   - Embedded payments and attachments for efficient retrieval
   - Flexible attributes for extensibility
   ```json
   {
       "address": {
           "street": string,
           "city": string,
           "state": string,
           "zipCode": string
       },
       "rentAmount": decimal,
       "leaseDates": {
           "startDate": DateTime,
           "endDate": DateTime
       },
       "payments": [{
           "amount": decimal,
           "date": DateTime,
           "method": string,
           "reference": string,
           "attachments": [/* attachment refs */]
       }],
       "attachments": [{
           "fileName": string,
           "contentType": string,
           "path": string,
           "size": long
       }],
       "attributes": { } // Flexible attributes
   }
   ```

### Key Features
- Document-based schema with embedded arrays for related data
- Optimistic concurrency using version field
- Compound indexes for efficient querying
- Text indexes for search functionality
- Flexible attributes dictionary for schema evolution
- Built-in multi-tenant data isolation

```mermaid
erDiagram
    User ||--|{ RentalProperty : "owns"
    User ||--|{ PaymentMethod : "has"
    RentalProperty ||--|{ RentalPayment : "has"
    PaymentMethod ||--|{ RentalPayment : "has"
    RentalProperty ||--|{ Attachment : "has"
    RentalPayment ||--|{ Attachment : "has"

    RentalPayment {
        uuid id
        uuid rentalPropertyId
        decimal amount
        datetime paymentDate
        uuid paymentMethodId
        string paymentReference
        string notes
        datetime createdAt
        datetime updatedAt
    }
    RentalProperty {
        uuid id
        uuid userId
        string address
        string suburb
        string state
        string postCode
        string description
        decimal weeklyRentAmount
        datetime leaseStartDate
        datetime leaseEndDate
        string propertyManager
        string propertyManagerContact
        datetime createdAt
        datetime updatedAt
    }

    User {
        uuid id
        string email
        string passwordHash
        int userType
        datetime createdAt
        datetime updatedAt
    }
    PaymentMethod {
        uuid id
        string name
        string description
        datetime createdAt
        datetime updatedAt
    }
    Attachment {
        uuid id
        string fileName
        string contentType
        string storagePath
        bigint fileSize
        string description
        string entityType
        uuid rentalPropertyId
        uuid rentalPaymentId
        datetime uploadDate
        array tags
    }
```

### Technology Stack

```mermaid
graph TD
    A[RentTracker] --> B[Backend]
    A --> C[Frontend]
    A --> D[Data Layer]
    A --> E[File Storage]
    
    B --> B1[ASP.NET Core]
    B --> B2[Minimal API]
    B --> B3[MongoDB.Driver]
    
    C --> C1[Blazor WebAssembly]
    C --> C2[.NET 8]
    C --> C3[Razor Components]
    
    D --> D1[MongoDB]
    D --> D2[Redis Cache]
    D --> D3[GridFS]
    
    E --> E1[File Service]
    E --> E2[Secure Storage]
```
* **Backend Framework**: ASP.NET Core minimal API (.NET 8)
* **Frontend Framework**: Blazor WebAssembly (.NET 8)
* **Authentication**: JWT-based authentication
* **Database**: MongoDB with Redis caching
* **Data Access**: MongoDB.Driver with optimized repositories
* **Architecture Pattern**: RESTful API with document-based storage
* **File Management**: Custom FileService implementation with GridFS
* **UI Components**: Modular Razor Components
* **Client Architecture**: Service-based with typed HTTP clients
* **Caching Layer**: Redis for high-performance data access

### Backend Implementation

#### Core Models

```mermaid
classDiagram
    class BaseDocument {
        +ObjectId Id
        +string TenantId
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +long Version
    }
    class User {
        <<extends BaseDocument>>
        +string Email
        +string PasswordHash
        +string UserType
        +Dictionary Attributes
    }
    class RentalProperty {
        <<extends BaseDocument>>
        +Address Address
        +decimal RentAmount
        +LeaseDates LeaseDates
        +List~Payment~ Payments
        +List~Attachment~ Attachments
        +Dictionary Attributes
    }
    class Address {
        +string Street
        +string City
        +string State
        +string ZipCode
    }
    class Payment {
        +decimal Amount
        +DateTime PaymentDate
        +string Method
        +string Reference
        +List~Attachment~ Attachments
    }
    class Attachment {
        +string FileName
        +string ContentType
        +string Path
        +long Size
    }
    BaseDocument <|-- User
    BaseDocument <|-- RentalProperty
    RentalProperty --> Address
    RentalProperty --> "*" Payment
    RentalProperty --> "*" Attachment
    Payment --> "*" Attachment
```

### Client Architecture

```mermaid
graph TD
    A[RentTrackerClient] --> B[Services]
    A --> C[Components]
    A --> D[Models]
    A --> E[Shared]
    
    B --> B1[RentalPropertyService]
    B --> B2[PaymentService]
    B --> B3[AttachmentService]
    
    C --> C1[Property Management]
    C --> C2[Payment Management]
    C --> C3[Document Management]
    
    C1 --> C1A[PropertyList]
    C1 --> C1B[PropertyEdit]
    C2 --> C2A[PaymentList]
    C2 --> C2B[PaymentEdit]
    
    D --> D1[DTOs]
    D --> D2[View Models]
    
    E --> E1[Layouts]
    E --> E2[UI Components]
```

#### Service Layer
- Typed HTTP clients for API communication
- Pagination support with generic response types
- Error handling and validation
- State management for UI components

#### Component Structure
- Modular design with separate components per feature
- Shared UI components for consistency
- Full-screen layouts with efficient navigation
- Property-specific payment context maintenance

#### State Management
- Local component state for UI interactions
- Service-level caching for API responses
- Pagination state management
- Loading and error states

## API Documentation

### Authentication and Authorization

```mermaid
sequenceDiagram
    participant Client
    participant Auth API
    participant Token Service
    participant Database
    
    Client->>+Auth API: POST /api/auth/register
    Auth API->>+Database: Create User
    Database-->>-Auth API: User Created
    Auth API-->>-Client: Success Response
    
    Client->>+Auth API: POST /api/auth/login
    Auth API->>+Database: Validate Credentials
    Database-->>-Auth API: User Valid
    Auth API->>+Token Service: Generate JWT
    Token Service-->>-Auth API: JWT Token
    Auth API-->>-Client: JWT Token Response
    
    Note over Client,Auth API: All subsequent requests include JWT
```

#### Authentication Endpoints

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|-----------|
| POST | `/api/auth/register` | Register new user | `{ "email": string, "password": string, "confirmPassword": string, "userType": "User"⎮"Admin" }` | Created user details |
| POST | `/api/auth/login` | Login user | `{ "email": string, "password": string }` | JWT token |

#### Multi-Tenancy Implementation

- All endpoints require JWT authentication token in the `Authorization: Bearer <token>` header
- User data is isolated using the `userId` from the JWT token
- Admin users can access system-wide resources
- Normal users can only access their own resources
- Property endpoints filter results by the authenticated user's ID
- System-wide payment methods are accessible to all users, while user-specific methods are isolated

### Property Management

#### Endpoints

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Cache
    participant MongoDB
    
    Client->>+API: GET /api/properties?page=1&pageSize=10
    API->>+Cache: Check Cache
    Cache-->>-API: Cache Miss
    API->>+MongoDB: Aggregate Pipeline Query
    Note over API: Apply pagination and filtering
    MongoDB-->>-API: Return Documents
    API->>Cache: Store in Cache
    API-->>-Client: Properties with Pagination Info

    Client->>+API: POST /api/properties
    Note over API: Generate ObjectId
    API->>+MongoDB: Insert Document
    MongoDB-->>-API: Document Created
    API->>Cache: Invalidate Cache
    API-->>-Client: Return Property Details

    Client->>+API: GET /api/properties/{id}
    API->>+Cache: Check Cache
    Cache-->>-API: Cache Hit
    API-->>-Client: Property Details
    
    Client->>+API: PUT /api/properties/{id}
    API->>+MongoDB: Update with Version Check
    Note over API: Using optimistic concurrency
    MongoDB-->>-API: Updated
    API->>Cache: Invalidate Cache
    API-->>-Client: Success Response

    Client->>+API: DELETE /api/properties/{id}
    API->>+MongoDB: Delete Document
    MongoDB-->>-API: Deleted
    API->>Cache: Invalidate Cache
    API-->>-Client: Success Response
```

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/properties` | List user's properties | `Bearer JWT` Required |
| GET | `/api/properties/{id}` | Get property details | `Bearer JWT` Required, Owner Only |
| POST | `/api/properties` | Create new property | `Bearer JWT` Required |
| PUT | `/api/properties/{id}` | Update property | `Bearer JWT` Required, Owner Only |
| DELETE | `/api/properties/{id}` | Delete property | `Bearer JWT` Required, Owner Only |

### Payment Management

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Cache
    participant MongoDB
    
    Client->>+API: GET /api/properties/{propertyId}/payments
    API->>+Cache: Check Cache
    Cache-->>-API: Cache Miss
    API->>+MongoDB: Find Document & Project Payments
    Note over API: Using array filters and projection
    MongoDB-->>-API: Return Payments Array
    API->>Cache: Store in Cache
    API-->>-Client: Payments with Pagination Info

    Client->>+API: POST /api/properties/{propertyId}/payments
    API->>+MongoDB: Update Array with New Payment
    Note over API: Using $push operator
    MongoDB-->>-API: Payment Added
    API->>Cache: Invalidate Cache
    API-->>-Client: 201 Created with Payment Details

    Client->>+API: PUT /api/properties/{propertyId}/payments/{paymentId}
    API->>+MongoDB: Update Array Element
    Note over API: Using $set with array filters
    MongoDB-->>-API: Payment Updated
    API->>Cache: Invalidate Cache
    API-->>-Client: Success Response
    
    Client->>+API: DELETE /api/properties/{propertyId}/payments/{paymentId}
    API->>+MongoDB: Update with $pull
    MongoDB-->>-API: Payment Removed
    API->>Cache: Invalidate Cache
    API-->>-Client: Success Response
```

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/properties/{propertyId}/payments?page={page}&pageSize={size}` | List paginated payments for a property | `Bearer JWT` Required, Property Owner Only |
| GET | `/api/properties/{propertyId}/payments/{paymentId}` | Get specific payment details | `Bearer JWT` Required, Property Owner Only |
| POST | `/api/properties/{propertyId}/payments` | Record new payment | `Bearer JWT` Required, Property Owner Only |
| PUT | `/api/properties/{propertyId}/payments/{paymentId}` | Update payment details | `Bearer JWT` Required, Property Owner Only |
| DELETE | `/api/properties/{propertyId}/payments/{paymentId}` | Delete payment record | `Bearer JWT` Required, Property Owner Only |

### Document Management

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant FileService
    participant Database
    
    Client->>+API: POST /api/properties/{id}/attachments
    API->>+FileService: Upload File
    FileService-->>-API: File Stored
    API->>+Database: Save Attachment Info
    Database-->>-API: Attachment Saved
    API-->>-Client: Success Response

    Client->>+API: GET /api/properties/{id}/attachments
    API->>+Database: Fetch Attachments
    Database-->>-API: Return Attachments
    API-->>-Client: Attachment List
```

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/properties/{id}/attachments` | List property attachments | `Bearer JWT` Required, Property Owner Only |
| POST | `/api/properties/{id}/attachments` | Upload attachment | `Bearer JWT` Required, Property Owner Only |
| GET | `/api/attachments/{id}` | Download attachment | `Bearer JWT` Required, Owner Only |
| DELETE | `/api/attachments/{id}` | Delete attachment | `Bearer JWT` Required, Owner Only |

## Setup Guide

### Prerequisites
- .NET 8 SDK (latest version)
- PostgreSQL database server (13.0 or higher)
- Storage location for file uploads (with proper permissions)
- Visual Studio Code (recommended) or Visual Studio 2022
- Git for version control
- Node.js and npm for frontend development tools

### Database Setup and Seeding
The application uses Entity Framework Core for database management and includes automatic migrations and data seeding:

#### Migrations
Migrations are automatically applied when the application starts:
```csharp
// In Program.cs
using (var scope = app.Services.CreateScope())
{
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

You can also manually apply migrations using:
```bash
cd RentTrackerBackend
dotnet ef database update
```

#### Sample Data
The application includes a `DatabaseSeeder` that automatically populates the database with sample data when it's empty:

- **Rental Properties**:
  * Sydney CBD apartment (2 bedrooms, $650/week)
  * Bondi beach house (3 bedrooms, $950/week)

- **Rental Payments**:
  * Initial payments including bonds
  * Regular rent payments with references

- **Sample Attachments**:
  * Lease agreements
  * Payment receipts

The seeder only runs if the database is empty, making it safe for production environments.

### Development Environment Setup
- Install Visual Studio Code and the C# Dev Kit extension
- Configure the following extensions:
  * C# Dev Kit for .NET development
  * .NET Core Test Explorer for running tests
  * GitLens for enhanced Git integration
  * REST Client for testing API endpoints
- Set up PostgreSQL and create a new database
- Configure file storage permissions

### Installation Steps

1. Clone the repository:
```bash
git clone https://github.com/yourusername/RentTracker.git
cd RentTracker
```

2. Configure the database connection:

Create or update `RentTrackerBackend/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=renttracker;Username=your_username;Password=your_password"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "renttracker",
    "Audience": "renttracker-client",
    "ExpiryInMinutes": 60
  }
}
```

For development, you can use user secrets to store sensitive configuration:
```bash
cd RentTrackerBackend
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=renttracker;Username=your_username;Password=your_password"
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key-here"
```

3. Verify database setup:
```bash
# Check database connection and migrations status
cd RentTrackerBackend
dotnet ef migrations list
dotnet ef database update --verbose
```

The application will automatically:
- Apply any pending migrations
- Create required directories for file uploads
- Seed sample data if the database is empty
- Create a default admin user if none exists

3. Run database migrations:
```bash
cd RentTrackerBackend
dotnet ef database update
```

4. Run the backend:
```bash
dotnet run
```

5. Run the frontend:
```bash
cd ../RentTrackerClient
dotnet run
```

The backend API will be available at `https://localhost:5001`, and the frontend at `https://localhost:5002`.

### Development Notes
- Enable hot reload for faster development iterations
- Use the integrated terminal for running commands
- Utilize the built-in debugger for both backend and frontend
- Configure user secrets for sensitive configuration
- Follow the code style and organization patterns
- Review multi-tenancy documentation in docs/multi-tenancy-plan.md

### Performance Considerations
- Backend Optimizations:
  * Lazy loading patterns for related data
  * AsNoTracking for read-only operations
  * Efficient query patterns with proper includes
  * Pagination for all list operations
  * Proper indexing on database tables

- Frontend Optimizations:
  * Component-level state management
  * Efficient API call patterns
  * Proper caching strategies
  * Optimized rendering with proper component lifecycle
  * Minimized re-renders using proper state management

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## Support and Contact

For technical questions or issues, please [open an issue](https://github.com/yourusername/RentTracker/issues) on GitHub.

For documentation on multi-tenancy implementation, refer to docs/multi-tenancy-plan.md.

For commercial support or custom development, contact: support@renttracker.com

## License

Apache 2.0 © 2024
