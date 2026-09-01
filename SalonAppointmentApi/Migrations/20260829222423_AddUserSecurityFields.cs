using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalonAppointmentApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSecurityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AppUsers_CustomerUserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_CustomerUserId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "CustomerUserId",
                table: "Reviews");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AppUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AppUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AppUsers",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AppUsers_CustomerId",
                table: "Reviews",
                column: "CustomerId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AppUsers_CustomerId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AppUsers");

            migrationBuilder.AddColumn<int>(
                name: "CustomerUserId",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerUserId",
                table: "Reviews",
                column: "CustomerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AppUsers_CustomerUserId",
                table: "Reviews",
                column: "CustomerUserId",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }
    }
}
