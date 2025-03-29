# Product Context
      
This file provides a high-level overview of the project and the expected product that will be created. Initially it is based upon projectBrief.md (if provided) and all other available project-related information in the working directory. This file is intended to be updated as the project evolves, and should be used to inform all other modes of the project's goals and context.
      
## Project Goal

* Implement a rental property management system with payment tracking and reporting features
* Provide landlords with an easy-to-use interface for managing their rental properties
* Track rental payments and maintain financial records
* Store and manage property-related documents and attachments

## Key Features

### Existing Features
* Property management (add, edit, delete properties)
* Payment tracking (record and manage rental payments)
* Document management (upload and download attachments)
* Basic property details (address, rent amount, lease dates, property manager info)

### Planned Features
* Implement multi tenancy with authentication
* Dockerize the project
* Enhanced reporting capabilities for financial analysis
* Dashboard with key metrics and visualizations
* Payment reminder system
* Improved data export functionality
* Advanced search and filtering options
* Enhanced API documentation and testing

## Overall Architecture

* ASP.NET Core minimal API (.NET 6+)
* Entity Framework Core with PostgreSQL database
* Three main models: RentalProperty, RentalPayment, and Attachment
* RESTful API endpoints for CRUD operations
* FileService for handling file uploads and downloads
