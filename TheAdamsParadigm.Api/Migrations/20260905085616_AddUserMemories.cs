using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheAdamsParadigm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMemories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: "orders" and "services" already exist in the database (created via
            // Data/InitialSchema.sql, not EF migrations). This is the first EF migration
            // ever generated for this project, so EF wants to recreate them too — that step
            // was removed by hand since it would fail with "relation already exists" against
            // the live database. Only the genuinely new table is created below.
            migrationBuilder.CreateTable(
                name: "user_memories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    chat_user_id = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_memories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_memories_chat_user_id",
                table: "user_memories",
                column: "chat_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_memories");
        }
    }
}
