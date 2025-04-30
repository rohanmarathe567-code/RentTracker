# RentTracker Backend Test Suite

This directory contains the complete test suite for the RentTracker backend API, following a comprehensive test pyramid approach.

## Directory Structure

### Unit Tests (`Unit/`)
- **Repositories/**: Tests for data access layer and MongoDB repository operations
- **Services/**: Tests for business logic and service layer operations
- **Endpoints/**: Tests for API endpoint handlers and request processing

### Integration Tests (`Integration/`)
- **Database/**: Tests for MongoDB integration using test containers
- **API/**: Tests for complete request-to-database flows using WebApplicationFactory

### End-to-End Tests (`E2E/`)
- **Workflows/**: Tests for complete business workflows and user scenarios
- **TestData/**: Test data builders and utilities for generating test data

### Security Tests (`Security/`)
- **Authorization/**: Tests for role-based access control and tenant isolation
- **Authentication/**: Tests for user authentication and token management

## Test Categories

1. **Unit Tests**: Fast, isolated tests for individual components
2. **Integration Tests**: Tests for component interactions and external dependencies
3. **E2E Tests**: Tests for complete business workflows
4. **Security Tests**: Tests for authentication, authorization, and security features