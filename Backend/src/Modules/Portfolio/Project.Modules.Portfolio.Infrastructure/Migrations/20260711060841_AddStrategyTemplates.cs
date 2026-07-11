using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStrategyTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "strategy_templates",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    goal_types = table.Column<List<string>>(type: "text[]", nullable: false),
                    risk_min = table.Column<int>(type: "integer", nullable: false),
                    risk_max = table.Column<int>(type: "integer", nullable: false),
                    requires_speculative_unlock = table.Column<bool>(type: "boolean", nullable: false),
                    buckets_json = table.Column<string>(type: "jsonb", nullable: false),
                    rebalance_cadence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    drawdown_alert_pct = table.Column<double>(type: "double precision", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_strategy_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_strategy_templates_key",
                schema: "Portfolio",
                table: "strategy_templates",
                column: "key",
                unique: true);

            // v1 template set (§ 3.2). Buckets are rules over registry
            // attributes, never symbols. Retirement's core bucket is the broad
            // equity ETF (EGX30 ETF replaces/joins SPY when EGX data lands);
            // tactical/speculative buckets fold into core until § 3.4 ships.
            var seededAt = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
            string[] cols = ["id", "key", "name", "goal_types", "risk_min", "risk_max", "requires_speculative_unlock", "buckets_json", "rebalance_cadence", "drawdown_alert_pct", "is_active", "created_at"];
            string[] colTypes = ["uuid", "character varying(50)", "character varying(100)", "text[]", "integer", "integer", "boolean", "jsonb", "character varying(20)", "double precision", "boolean", "timestamp with time zone"];

            migrationBuilder.InsertData(
                schema: "Portfolio",
                table: "strategy_templates",
                columns: cols,
                columnTypes: colTypes,
                values: new object[,]
                {
                    {
                        new Guid("a3b20000-0000-0000-0000-000000000001"),
                        "retirement_set_and_forget", "Retirement / Set-and-Forget",
                        new[] { "Retirement" }, 0, 100, false,
                        "[{\"sleeve\":\"core\",\"weight\":0.40,\"rules\":{\"assetClasses\":[\"equity\"],\"types\":[\"etf\"]}},"
                        + "{\"sleeve\":\"stability\",\"weight\":0.25,\"rules\":{\"assetClasses\":[\"gold\"]}},"
                        + "{\"sleeve\":\"stability\",\"weight\":0.20,\"rules\":{\"assetClasses\":[\"fixed_income\"]}},"
                        + "{\"sleeve\":\"stability\",\"weight\":0.15,\"rules\":{\"assetClasses\":[\"cash_like\"]}}]",
                        "semi_annual", 0.15, true, seededAt
                    },
                    {
                        new Guid("a3b20000-0000-0000-0000-000000000002"),
                        "balanced_growth", "Balanced Growth",
                        new[] { "LongTermWealth", "MediumTermGoal", "SpeculationLearning" }, 0, 69, false,
                        "[{\"sleeve\":\"core\",\"weight\":0.50,\"rules\":{\"types\":[\"stock\"]}},"
                        + "{\"sleeve\":\"stability\",\"weight\":0.30,\"rules\":{\"assetClasses\":[\"gold\",\"fixed_income\"]}},"
                        + "{\"sleeve\":\"stability\",\"weight\":0.20,\"rules\":{\"assetClasses\":[\"cash_like\"]}}]",
                        "monthly", 0.12, true, seededAt
                    },
                    {
                        new Guid("a3b20000-0000-0000-0000-000000000003"),
                        "active_growth", "Active Growth",
                        new[] { "LongTermWealth", "MediumTermGoal", "SpeculationLearning" }, 70, 100, false,
                        "[{\"sleeve\":\"core\",\"weight\":0.50,\"rules\":{\"types\":[\"stock\"]}},"
                        + "{\"sleeve\":\"tactical\",\"weight\":0.30,\"rules\":{\"types\":[\"stock\"]}},"
                        + "{\"sleeve\":\"speculative\",\"weight\":0.10,\"rules\":{\"types\":[\"stock\"]}},"
                        + "{\"sleeve\":\"stability\",\"weight\":0.10,\"rules\":{\"assetClasses\":[\"cash_like\"]}}]",
                        "weekly", 0.20, true, seededAt
                    },
                    {
                        new Guid("a3b20000-0000-0000-0000-000000000004"),
                        "speculative_gated", "Speculative (Gated)",
                        new[] { "SpeculationLearning" }, 70, 100, true,
                        "[{\"sleeve\":\"core\",\"weight\":0.40,\"rules\":{\"types\":[\"stock\"]}},"
                        + "{\"sleeve\":\"tactical\",\"weight\":0.30,\"rules\":{\"types\":[\"stock\"]}},"
                        + "{\"sleeve\":\"speculative\",\"weight\":0.20,\"rules\":{\"types\":[\"stock\"]}},"
                        + "{\"sleeve\":\"stability\",\"weight\":0.10,\"rules\":{\"assetClasses\":[\"cash_like\"]}}]",
                        "weekly", 0.25, true, seededAt
                    },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "strategy_templates",
                schema: "Portfolio");
        }
    }
}
