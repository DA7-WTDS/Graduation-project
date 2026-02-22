using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Portfolio");

            migrationBuilder.CreateTable(
                name: "inbox_message_consumers",
                schema: "Portfolio",
                columns: table => new
                {
                    inbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_message_consumers", x => new { x.inbox_message_id, x.name });
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "jsonb", maxLength: 2000, nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_message_consumers",
                schema: "Portfolio",
                columns: table => new
                {
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_message_consumers", x => new { x.outbox_message_id, x.name });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "jsonb", maxLength: 2000, nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "portfolios",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    primary_goal = table.Column<string>(type: "text", nullable: false),
                    time_horizon = table.Column<string>(type: "text", nullable: false),
                    risk_tolerance = table.Column<int>(type: "integer", nullable: false),
                    market_reaction = table.Column<string>(type: "text", nullable: false),
                    investment_experience = table.Column<string>(type: "text", nullable: false),
                    stocks_percentage = table.Column<int>(type: "integer", nullable: false),
                    bonds_percentage = table.Column<int>(type: "integer", nullable: false),
                    etfs_percentage = table.Column<int>(type: "integer", nullable: false),
                    cash_percentage = table.Column<int>(type: "integer", nullable: false),
                    risk_profile = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portfolios", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_processed_on_utc_occurred_on_utc",
                schema: "Portfolio",
                table: "inbox_messages",
                columns: new[] { "processed_on_utc", "occurred_on_utc" });

            migrationBuilder.CreateIndex(
                name: "idx_outbox_messages_unprocessed",
                schema: "Portfolio",
                table: "outbox_messages",
                columns: new[] { "occurred_on_utc", "processed_on_utc" },
                filter: "processed_on_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_portfolios_user_id",
                schema: "Portfolio",
                table: "portfolios",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_message_consumers",
                schema: "Portfolio");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "Portfolio");

            migrationBuilder.DropTable(
                name: "outbox_message_consumers",
                schema: "Portfolio");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "Portfolio");

            migrationBuilder.DropTable(
                name: "portfolios",
                schema: "Portfolio");
        }
    }
}
