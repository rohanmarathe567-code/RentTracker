using Microsoft.EntityFrameworkCore.Migrations;

namespace RentTrackerBackend.Migrations;

public class CustomMigrationOperation : IDisposable
{
    private readonly MigrationBuilder _migrationBuilder;
    
    public CustomMigrationOperation(MigrationBuilder migrationBuilder)
    {
        _migrationBuilder = migrationBuilder;
    }
    
    public void Up()
    {
        // Add user table first
        _migrationBuilder.CreateTable(
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

        // Add unique index on Email
        _migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);
            
        // Seed default user
        SeedDefaultUser.AddDefaultUser(_migrationBuilder);
        
        // Add UserId column to RentalProperties
        _migrationBuilder.AddColumn<Guid>(
            name: "UserId",
            table: "RentalProperties",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("71C19B83-9720-4446-A955-EA729D2284B5")); // Default admin user ID
            
        // Add foreign key
        _migrationBuilder.AddForeignKey(
            name: "FK_RentalProperties_Users_UserId",
            table: "RentalProperties",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
    
    public void Down()
    {
        _migrationBuilder.DropForeignKey(
            name: "FK_RentalProperties_Users_UserId",
            table: "RentalProperties");
            
        _migrationBuilder.DropColumn(
            name: "UserId",
            table: "RentalProperties");
            
        _migrationBuilder.DropTable(
            name: "Users");
    }
    
    public void Dispose()
    {
        // No resources to dispose
    }
}