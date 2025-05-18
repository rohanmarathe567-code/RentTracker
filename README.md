# RentTracker

[![Project Status: Active](https://img.shields.io/badge/status-active-brightgreen.svg)](https://github.com/rahulbedge/RentTracker)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![build](https://github.com/rahulbedge/RentTracker/actions/workflows/pr-build.yml/badge.svg?branch=master)](https://github.com/rahulbedge/RentTracker/actions/workflows/pr-build.yml)

A comprehensive multi-tenant property management system for tracking income, expenses, and managing property-related documents with advanced financial reporting capabilities.

## Overview

RentTracker is a modern property management solution built with ASP.NET Core that helps property owners efficiently manage their rental properties and finances. The system provides robust transaction management with support for both income and expenses, comprehensive financial reporting, document management, and multi-tenant capabilities. Built on MongoDB for flexibility and Redis for performance, it offers secure data storage with advanced querying and real-time analytics features.

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

### Current Features
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
* Advanced Transaction Management
   - Support for both income and expenses
   - Transaction categorization system
   - Financial summaries with category breakdowns
   - Date range filtering and reporting
   - Optimistic concurrency control
   - Rich transaction history with document versioning
* Theme System Enhancements
  - Dark theme support
  - Theme customization interface
  - Theme transition animations
  - User theme preferences

### Planned Features
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
* MongoDB Performance Optimization
  - Advanced aggregation pipelines for reporting
  - Enhanced MongoDB-specific caching strategies
  - Optimized query patterns
  - Real-time updates using change streams
* Testing and Documentation
  - Comprehensive unit test coverage
  - Updated API documentation for MongoDB features
  - Multi-tenant integration tests
  - Enhanced security testing
* Future Enhancements
  - Docker containerization with CI/CD pipeline
  - Enhanced reporting with MongoDB aggregation
  - Dashboard with metrics visualization
  - Payment reminder system
  - Advanced search and filtering
  - Improved data export functionality

## Project Status and Roadmap

### Current Status
- ✅ MongoDB Migration Complete
- ✅ Optimized MongoDB Indexing
- ✅ Multi-tenancy Enhancement
- ✅ MongoDB Performance Optimization
- ✅ Theme System Implementation
- ⏳ Redis Cache Integration
- ⏳ Enhanced Document-based Storage
- ⏳ GridFS Implementation
- ⏳ Docker Containerization (Planned)
- ⏳ Advanced Reporting (Planned)
- ⏳ Payment Reminder System (Planned)

### Recent Achievements
- Successfully migrated from PostgreSQL to MongoDB
- Implemented Redis caching layer
- Enhanced data model with document-based design
- Optimized MongoDB indexes and queries
- Updated tenant isolation using MongoDB features
- Enhanced property attributes handling
- Improved API performance with caching

### Upcoming Milestones
1. Q2 2025: MongoDB Performance Optimization and Theme System
2. Q3 2025: Docker Containerization and CI/CD Pipeline
3. Q4 2025: Enhanced Reporting and Analytics

## Architecture

### Database Schema

The system uses MongoDB collections with a flexible, document-based data model:

#### Base Document Structure
```json
{
    "_id": ObjectId,
    "tenantId": string,
    "createdAt": DateTime,
    "updatedAt": DateTime,
    "version": long,
    "type": "income" | "expense",
    "categoryId": ObjectId,
    "attachments": [ObjectId]
}
```

#### Core Collections

1. **Users**:
```json
{
    "email": string,
    "passwordHash": string,
    "userType": string,
    "attributes": { }
}
```

2. **PaymentMethods**:
```json
{
    "name": string,
    "description": string,
    "isActive": boolean,
    "requiresReference": boolean,
    "attributes": { }
}
```

3. **Attachments**:
```json
{
    "originalFileName": string,
    "storedFileName": string,
    "contentType": string,
    "size": long,
    "uploadDate": DateTime,
    "userId": string,
    "metadata": {
        "propertyId": string,
        "paymentId": string,
        "category": string
    }
}
```

4. **RentalProperties**:
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
    "propertyManager": {
        "name": string,
        "contact": string
    },
    "description": string,
    "transactions": [{
        "amount": decimal,
        "date": DateTime,
        "type": "income" | "expense",
        "categoryId": ObjectId,
        "method": string,
        "reference": string,
        "notes": string,
        "attachments": [/* attachment refs */]
    }],
    "attachments": [{
        "fileName": string,
        "contentType": string,
        "path": string,
        "size": long
    }],
    "attributes": { }
}
```

### Technology Stack

- **Backend**: ASP.NET Core minimal API (.NET 8)
- **Database**: MongoDB with Redis caching
- **File Storage**: GridFS for document management
- **Authentication**: JWT-based with tenant isolation
- **Performance**: 
  * MongoDB aggregation pipelines
  * Redis caching layer
  * Optimized indexes
  * Cursor-based pagination

## API Documentation

Our comprehensive API documentation is available at `/api/docs` when running the application. It includes detailed endpoint descriptions, request/response examples, and authentication information.

For detailed architectural decisions and implementation plans, please refer to the following documentation:
- `docs/multi-tenancy-plan.md`
- `docs/mongodb-migration-plan.md`
- `docs/redis-cache-plan.md`

## Contributing

Please read our contributing guidelines in CONTRIBUTING.md before submitting pull requests.

## License

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
    class PropertyTransaction {
        +ObjectId Id
        +decimal Amount
        +DateTime TransactionDate
        +string Type
        +ObjectId CategoryId
        +string PaymentMethod
        +string Reference
        +string Notes
        +List~Attachment~ Attachments
        +Dictionary~string,object~ Attributes
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
    RentalProperty --> "*" PropertyTransaction
    RentalProperty --> "*" Attachment
    PropertyTransaction --> "*" Attachment
```

### Client Architecture

```mermaid
graph TD
    A[RentTrackerClient] --> B[Services]
    A --> C[Components]
    A --> D[Models]
    A --> E[Shared]
    
    B --> B1[RentalPropertyService]
    B --> B2[TransactionService]
    B --> B3[CategoryService]
    B --> B4[AttachmentService]
    
    C --> C1[Property Management]
    C --> C2[Transaction Management]
    C --> C3[Financial Reports]
    C --> C4[Document Management]
    
    C1 --> C1A[PropertyList]
    C1 --> C1B[PropertyEdit]
    C2 --> C2A[TransactionList]
    C2 --> C2B[TransactionEdit]
    C3 --> C3A[FinancialSummary]
    C3 --> C3B[CategoryBreakdown]
    
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
- Real-time updates integration
- Caching strategies with service workers

#### Component Structure
- Modular design with separate components per feature
- Shared UI components for consistency
- Full-screen layouts with efficient navigation
- Property-specific transaction context
- Financial reporting components
- Category management interfaces

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

### Transaction Management

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Cache
    participant MongoDB
    
    Client->>+API: GET /api/properties/{propertyId}/transactions
    API->>+Cache: Check Cache
    Cache-->>-API: Cache Miss
    API->>+MongoDB: Find Transactions
    Note over API: Apply filtering and pagination
    MongoDB-->>-API: Return Transactions
    API->>Cache: Store in Cache
    API-->>-Client: Transactions with Pagination Info

    Client->>+API: POST /api/properties/{propertyId}/transactions
    API->>+MongoDB: Create New Transaction
    Note over API: With type and category
    MongoDB-->>-API: Transaction Created
    API->>Cache: Invalidate Cache
    API-->>-Client: 201 Created with Details

    Client->>+API: GET /api/properties/{propertyId}/financial-summary
    API->>+MongoDB: Aggregate Transactions
    Note over API: Calculate totals by category
    MongoDB-->>-API: Summary Data
    API-->>-Client: Financial Summary Response
```

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/properties/{propertyId}/transactions` | List transactions with filtering | `Bearer JWT` Required, Owner Only |
| GET | `/api/properties/{propertyId}/transactions/{id}` | Get transaction details | `Bearer JWT` Required, Owner Only |
| POST | `/api/properties/{propertyId}/transactions` | Record new transaction | `Bearer JWT` Required, Owner Only |
| PUT | `/api/properties/{propertyId}/transactions/{id}` | Update transaction | `Bearer JWT` Required, Owner Only |
| DELETE | `/api/properties/{propertyId}/transactions/{id}` | Delete transaction | `Bearer JWT` Required, Owner Only |
| GET | `/api/properties/{propertyId}/financial-summary` | Get financial summary | `Bearer JWT` Required, Owner Only |
| GET | `/api/properties/{propertyId}/transaction-categories` | List transaction categories | `Bearer JWT` Required |

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
- Docker Desktop for running MongoDB and Redis
- MongoDB Compass (optional, for database management)
- Visual Studio Code (recommended) or Visual Studio 2022
- Git for version control

### Database Setup and Seeding
MongoDB collections and indexes are automatically created on startup:

#### Collections Setup
The following collections are created with appropriate indexes:
- Users
- RentalProperties
- TransactionCategories
- PaymentMethods
- Attachments

#### Sample Data
The application includes a data seeder that populates MongoDB with initial data:

- **Transaction Categories**:
  * Income: Rent, Bond, Other Income
  * Expenses: Maintenance, Insurance, Utilities, Other Expenses

- **Properties with Transactions**:
  * Sydney CBD apartment
    - Monthly rent income
    - Quarterly maintenance expenses
  * Bondi beach house
    - Weekly rent payments
    - Annual insurance costs

- **Sample Documents**:
  * Lease agreements
  * Insurance policies
  * Payment receipts
  * Maintenance invoices

Data seeding runs only in development and can be triggered via API endpoint

### Development Environment Setup
- Install Visual Studio Code and the C# Dev Kit extension
- Configure the following extensions:
  * C# Dev Kit for .NET development
  * .NET Core Test Explorer for running tests
  * GitLens for enhanced Git integration
  * REST Client for testing API endpoints
- Set up MongoDB and Redis using Docker Compose
- Configure GridFS storage permissions

### Installation Steps

1. Clone the repository:
```bash
git clone https://github.com/yourusername/RentTracker.git
cd RentTracker
```

2. Start MongoDB and Redis:
```bash
docker-compose up -d
```

3. Configure the application settings:

Create or update `RentTrackerBackend/appsettings.json`:
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://root:example@localhost:27017",
    "DatabaseName": "RentTracker"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "renttracker",
    "Audience": "renttracker-client",
    "ExpiryInMinutes": 60
  }
}
```

For development, you can use user secrets:
```bash
cd RentTrackerBackend
dotnet user-secrets init
dotnet user-secrets set "MongoDb:ConnectionString" "mongodb://root:example@localhost:27017"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379"
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key-here"
```

The application will automatically:
- Create required MongoDB collections
- Set up indexes for optimal performance
- Initialize Redis cache
- Create a default admin user if none exists

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
  * MongoDB aggregation pipelines for analytics
  * Redis caching for frequent queries
  * Optimized MongoDB indexes
  * Cursor-based pagination
  * Bulk operations for better performance

- Frontend Optimizations:
  * Component-level state management
  * Efficient API call patterns
  * Client-side caching with service workers
  * Optimized rendering with proper component lifecycle
  * Real-time updates using SignalR when available

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
