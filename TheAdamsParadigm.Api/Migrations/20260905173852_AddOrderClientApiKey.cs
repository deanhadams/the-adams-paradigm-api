using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheAdamsParadigm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderClientApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_api_key",
                table: "orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_api_key",
                table: "orders");
        }
    }
}
