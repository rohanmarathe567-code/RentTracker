using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RentTrackerBackend.Migrations;

public partial class AddUserAuthentication : Migration
{
    private readonly Guid _defaultUserId = new Guid("71C19B83-9720-4446-A955-EA729D2284B5");
    
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create Users table
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                UserType = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });
            
        // Add Email unique index
        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);
            
        // Add UserId column as nullable
        migrationBuilder.AddColumn<Guid>(
            name: "UserId",
            table: "RentalProperties",
            type: "uuid",
            nullable: true);
            
        // Add foreign key
        migrationBuilder.AddForeignKey(
            name: "FK_RentalProperties_Users_UserId",
            table: "RentalProperties",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
            
        // Insert default admin user
        migrationBuilder.InsertData(
            table: "Users",
            columns: new[] { "Id", "Email", "PasswordHash", "UserType", "CreatedAt", "UpdatedAt" },
            values: new object[] {
                _defaultUserId,
                "admin@renttracker.com",
                BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                1, // Admin
                DateTime.UtcNow,
                DateTime.UtcNow
            });
            
        // Update existing properties to use default user
        migrationBuilder.Sql($@"
            UPDATE ""RentalProperties""
            SET ""UserId"" = '{_defaultUserId}'
            WHERE ""UserId"" IS NULL;
        ");
        
        // Make UserId required
        migrationBuilder.AlterColumn<Guid>(
            name: "UserId",
            table: "RentalProperties",
            type: "uuid",
            nullable: false,
            defaultValue: _defaultUserId,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_RentalProperties_Users_UserId",
            table: "RentalProperties");
            
        migrationBuilder.DropTable(
            name: "Users");
            
        migrationBuilder.DropColumn(
            name: "UserId",
            table: "RentalProperties");
    }
}