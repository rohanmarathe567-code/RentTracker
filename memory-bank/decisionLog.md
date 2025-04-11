# Decision Log

This file records architectural and implementation decisions using a list format.

## 2025-04-10 22:28 - Multi-Tenancy Architecture Design

### Decision
Implement multi-tenancy support with two user types (Admin and Normal users) using JWT-based authentication and role-based access control.

### Rationale
- Need to support multiple users accessing their own properties
- Require admin capabilities for system configuration
- Data isolation between users is critical for security and privacy
- JWT provides secure, stateless authentication

### Implementation Details
- New User entity with role-based access control
- Database schema changes to support user ownership
- JWT-based authentication system
- Data isolation through user context in queries
- Detailed implementation plan created in memory-bank/multi-tenancy-plan.md

## 2025-04-12 00:07 - Authentication Architecture Decision

### Decision
Maintain authentication within the main project rather than separating it into a distinct service.

### Rationale
- Authentication is tightly integrated with multi-tenancy implementation
- Current code organization is clean and well-structured
- Separation would add unnecessary complexity to data access patterns
- User entity relationships are efficiently managed within the same context

### Implementation Details
- Continue with current auth implementation in main project
- Focus on completing pending security enhancements
- Maintain clear boundaries and documentation for future flexibility
- Detailed analysis available in memory-bank/auth-separation-analysis.md

### Future Reconsideration Triggers
- Increased auth complexity
- Need to support multiple applications
- Separate scaling requirements
- SSO or external auth provider implementation needs
