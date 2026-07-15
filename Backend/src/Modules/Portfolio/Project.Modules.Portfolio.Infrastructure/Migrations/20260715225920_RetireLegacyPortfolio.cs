using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RetireLegacyPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "investment_amount",
                schema: "Portfolio",
                table: "goals",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Backfill from the suitability record before the legacy row goes:
            // each goal's amount is in its newest questionnaire submission, so
            // existing goals keep their real amount instead of defaulting to 0.
            migrationBuilder.Sql("""
                UPDATE "Portfolio".goals g
                SET investment_amount = COALESCE((
                    SELECT (r.answers_json->>'investmentAmount')::numeric
                    FROM "Portfolio".questionnaire_responses r
                    WHERE r.goal_id = g.id
                    ORDER BY r.submitted_at DESC
                    LIMIT 1
                ), 0);
                """);

            // The goal-based model (goals + proposals + goal_portfolios) is now
            // the single source of truth; the old one-row-per-user portfolio is
            // fully superseded (Phase 4.7).
            migrationBuilder.DropTable(
                name: "portfolios",
                schema: "Portfolio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "investment_amount",
                schema: "Portfolio",
                table: "goals");

            migrationBuilder.CreateTable(
                name: "portfolios",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bonds_percentage = table.Column<int>(type: "integer", nullable: false),
                    cash_percentage = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    etfs_percentage = table.Column<int>(type: "integer", nullable: false),
                    investment_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    investment_experience = table.Column<string>(type: "text", nullable: false),
                    market_reaction = table.Column<string>(type: "text", nullable: false),
                    primary_goal = table.Column<string>(type: "text", nullable: false),
                    risk_profile = table.Column<int>(type: "integer", nullable: false),
                    risk_tolerance = table.Column<int>(type: "integer", nullable: false),
                    stocks_percentage = table.Column<int>(type: "integer", nullable: false),
                    time_horizon = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portfolios", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_portfolios_user_id",
                schema: "Portfolio",
                table: "portfolios",
                column: "user_id",
                unique: true);
        }
    }
}
