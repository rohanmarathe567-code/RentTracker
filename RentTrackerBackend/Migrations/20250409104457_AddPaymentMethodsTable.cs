using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentTrackerBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create PaymentMethods table
            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            var bankTransferId = Guid.NewGuid(); // Placeholder GUID for existing records

            // 3. Add PaymentMethodId column
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentMethodId",
                table: "RentalPayments",
                type: "uuid",
                nullable: false,
                defaultValue: bankTransferId); // Placeholder GUID for existing records that will be updated by DatabaseSeeder

            // 4. Create index for foreign key
            migrationBuilder.CreateIndex(
                name: "IX_RentalPayments_PaymentMethodId",
                table: "RentalPayments",
                column: "PaymentMethodId");

            // 5. Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_RentalPayments_PaymentMethods_PaymentMethodId",
                table: "RentalPayments",
                column: "PaymentMethodId",
                principalTable: "PaymentMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 6. Drop old PaymentMethod column
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "RentalPayments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RentalPayments_PaymentMethods_PaymentMethodId",
                table: "RentalPayments");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_RentalPayments_PaymentMethodId",
                table: "RentalPayments");

            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                table: "RentalPayments");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "RentalPayments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
