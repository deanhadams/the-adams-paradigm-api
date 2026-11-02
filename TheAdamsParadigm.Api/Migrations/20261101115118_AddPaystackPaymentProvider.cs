using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheAdamsParadigm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaystackPaymentProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_provider",
                table: "orders",
                type: "text",
                nullable: false,
                defaultValue: "Yoco");

            migrationBuilder.AddColumn<string>(
                name: "paystack_reference",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_orders_paystack_reference",
                table: "orders",
                column: "paystack_reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_orders_paystack_reference",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "payment_provider",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "paystack_reference",
                table: "orders");
        }
    }
}
