using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentTrackerBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddTenancyRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemDefault",
                table: "PaymentMethods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PaymentMethods",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_RentalPaymentId",
                table: "Attachments",
                column: "RentalPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_RentalPropertyId",
                table: "Attachments",
                column: "RentalPropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_RentalPayments_RentalPaymentId",
                table: "Attachments",
                column: "RentalPaymentId",
                principalTable: "RentalPayments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_RentalProperties_RentalPropertyId",
                table: "Attachments",
                column: "RentalPropertyId",
                principalTable: "RentalProperties",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_RentalPayments_RentalPaymentId",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_RentalProperties_RentalPropertyId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_RentalPaymentId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_RentalPropertyId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "IsSystemDefault",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PaymentMethods");
        }
    }
}
