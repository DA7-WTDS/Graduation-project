using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Recommendations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Recommendations");

            migrationBuilder.CreateTable(
                name: "daily_runs",
                schema: "Recommendations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_message_consumers",
                schema: "Recommendations",
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
                schema: "Recommendations",
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
                schema: "Recommendations",
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
                schema: "Recommendations",
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
                name: "stock_predictions",
                schema: "Recommendations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    daily_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    change_pct = table.Column<double>(type: "double precision", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    sentiment_score = table.Column<double>(type: "double precision", nullable: false),
                    signal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    analyst_rating = table.Column<double>(type: "double precision", nullable: true),
                    rating_label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    pt_upside_pct = table.Column<double>(type: "double precision", nullable: true),
                    news_score = table.Column<double>(type: "double precision", nullable: true),
                    agreement = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    risk_level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    conviction_score = table.Column<double>(type: "double precision", nullable: false),
                    risk_flags = table.Column<string[]>(type: "text[]", nullable: false),
                    rationale = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_predictions", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_predictions_daily_runs_daily_run_id",
                        column: x => x.daily_run_id,
                        principalSchema: "Recommendations",
                        principalTable: "daily_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_daily_runs_generated_at",
                schema: "Recommendations",
                table: "daily_runs",
                column: "generated_at");

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_processed_on_utc_occurred_on_utc",
                schema: "Recommendations",
                table: "inbox_messages",
                columns: new[] { "processed_on_utc", "occurred_on_utc" });

            migrationBuilder.CreateIndex(
                name: "idx_outbox_messages_unprocessed",
                schema: "Recommendations",
                table: "outbox_messages",
                columns: new[] { "occurred_on_utc", "processed_on_utc" },
                filter: "processed_on_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_predictions_daily_run_id",
                schema: "Recommendations",
                table: "stock_predictions",
                column: "daily_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_predictions_risk_level",
                schema: "Recommendations",
                table: "stock_predictions",
                column: "risk_level");

            migrationBuilder.CreateIndex(
                name: "ix_stock_predictions_ticker",
                schema: "Recommendations",
                table: "stock_predictions",
                column: "ticker");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_message_consumers",
                schema: "Recommendations");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "Recommendations");

            migrationBuilder.DropTable(
                name: "outbox_message_consumers",
                schema: "Recommendations");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "Recommendations");

            migrationBuilder.DropTable(
                name: "stock_predictions",
                schema: "Recommendations");

            migrationBuilder.DropTable(
                name: "daily_runs",
                schema: "Recommendations");
        }
    }
}
