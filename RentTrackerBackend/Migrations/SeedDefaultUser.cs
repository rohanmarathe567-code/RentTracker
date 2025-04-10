using Microsoft.EntityFrameworkCore.Migrations;
using RentTrackerBackend.Models.Auth;

namespace RentTrackerBackend.Migrations;

public static class SeedDefaultUser
{
    public static void AddDefaultUser(MigrationBuilder migrationBuilder)
    {
        // Create default admin user
        var defaultUserId = "71C19B83-9720-4446-A955-EA729D2284B5";
        migrationBuilder.Sql($@"
            INSERT INTO ""Users"" (""Id"", ""Email"", ""PasswordHash"", ""UserType"", ""CreatedAt"", ""UpdatedAt"")
            VALUES (
                '{defaultUserId}',
                'admin@renttracker.com',
                '$2a$11$HWN3FhzTrOtJEwsxS1dkDOXeM5vwc9O3oMbQEUXIWNQOQNxOJKnSi',  -- Password: Admin123!
                {(int)UserType.Admin},
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP
            );
        ");
        
        // Update existing RentalProperties to reference the default user
        migrationBuilder.Sql($@"
            UPDATE ""RentalProperties""
            SET ""UserId"" = '{defaultUserId}'
            WHERE ""UserId"" IS NULL;
        ");
    }
}