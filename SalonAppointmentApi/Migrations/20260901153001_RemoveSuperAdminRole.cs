using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalonAppointmentApi.Migrations
{
    public partial class RemoveSuperAdminRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE `AppUsers` SET `Role` = 'Admin', `UpdatedAt` = CURRENT_TIMESTAMP(6) WHERE `Role` = 'SuperAdmin';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally irreversible: converted users are valid Admin accounts.
        }
    }
}
