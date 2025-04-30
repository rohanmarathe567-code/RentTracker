[2025-04-26 19:30:00] - Backend Service Layer Enhancement

## Decision
Implement dedicated service layer and improve dependency injection across the backend.

## Rationale
1. Previous implementation:
   - Business logic mixed in controllers
   - Repeated authorization logic
   - Direct repository usage in controllers
   - Tight coupling between components

2. New implementation:
   - Dedicated service layer for business logic
   - Centralized claims handling via ClaimsPrincipalService
   - Improved separation of concerns
   - Better testability and maintainability

## Implementation Details
1. Added ClaimsPrincipalService:
   - Centralized tenant ID validation
   - Unified role checking
   - Consistent claims access

2. Added PropertyService:
   - Encapsulated property-related business logic
   - Implemented proper pagination and sorting
   - Centralized property validation
   - Improved error handling

3. Updated Controllers:
   - Removed business logic
   - Use dependency injection for services
   - Simplified endpoint implementations
   - Better error handling

## Impact
- Improved code organization
- Better separation of concerns
- Enhanced testability
- Reduced code duplication
- More maintainable codebase
- Clearer responsibility boundaries
# Decision Log

This file records architectural and implementation decisions using a list format.
2025-04-22 18:51:00 - Initial file creation and first decision logged.

## Integration Testing Framework Selection

**Decision**: Switch from SpecFlow to LightBDD for BDD implementation

**Date**: 2025-04-22

**Context**:
- SpecFlow has been discontinued
- Need a modern, actively maintained BDD framework for .NET
- Must maintain existing BDD scenarios and structure
- Integration with CI/CD and reporting is essential

**Rationale**:
- LightBDD is actively maintained with regular updates
- Native .NET Core support ensures long-term viability
- Built-in HTML report generation streamlines CI/CD integration
- Fluent API provides clear and maintainable test definitions
- Good integration with popular test frameworks (xUnit/NUnit)
- Async/await support for modern testing patterns

**Implementation Details**:
- Created dedicated integration-tests-bdd-plan.md in memory-bank
- Will use LightBDD as the primary BDD framework
- Maintain domain-driven structure (Repository, API, Security)
- Leverage LightBDD's built-in reporting capabilities
- Integrate with existing test fixtures using LightBDD's API

## Previous Integration Testing Approach

**Decision**: Implement integration tests using Behavior-Driven Development (BDD)

**Date**: 2025-04-22

**Context**:
- Need to implement comprehensive integration tests
- Multiple stakeholders need to understand test scenarios
- Complex business workflows need to be tested
- Want to improve collaboration between technical and non-technical team members

**Rationale**:
- BDD provides natural language specifications that both technical and non-technical stakeholders can understand
- Helps maintain living documentation that stays in sync with the codebase
- Provides clear structure for testing complex business workflows
- Improves collaboration between developers, QA, and product owners
- Allows for better traceability between requirements and tests

**Impact**:
- Integration tests will be more maintainable and understandable
- Better alignment between acceptance criteria and test scenarios
- Improved documentation of system behavior
- Enhanced collaboration in test planning and review

[2025-04-22 19:07:23] - Deleted unit-tests-plan.md as it contained primarily performance test scenarios and monitoring plans rather than unit tests. This will be revisited later when focusing specifically on performance testing.
