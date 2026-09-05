using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheAdamsParadigm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClientICloudCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "icloud_calendar",
                table: "clients",
                type: "text",
                nullable: false,
                defaultValue: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "icloud_calendar",
                table: "clients");
        }
    }
}
