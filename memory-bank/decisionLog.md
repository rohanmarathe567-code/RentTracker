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
- Detailed implementation plan created in docs/multi-tenancy-plan.md
