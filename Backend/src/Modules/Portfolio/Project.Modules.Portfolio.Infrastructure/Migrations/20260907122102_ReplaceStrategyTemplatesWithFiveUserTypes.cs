using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Portfolio.Infrastructure.Migrations
{
    /// <summary>
    /// Replaces the original four strategy templates with five covering distinct investor
    /// types, so the § C replay measures genuinely different portfolio shapes rather than
    /// four variations on the same one.
    ///
    /// The five differ on the axes that actually change a portfolio: equity exposure
    /// (20% to 90%), passive index versus ranked stock picking, stability composition,
    /// rebalance cadence (semi-annual to weekly) and drawdown tolerance.
    ///
    /// Shadow portfolios key off TemplateKey with no foreign key, so any shadow history
    /// under a removed key is orphaned rather than deleted. Deliberate: destroying a
    /// track record from a schema migration would be far worse than leaving rows behind,
    /// and ShadowPortfolioJob simply creates fresh portfolios for the new keys.
    /// </summary>
    public partial class ReplaceStrategyTemplatesWithFiveUserTypes : Migration
    {
        private static readonly string[] Columns =
        {
            "id", "key", "name", "goal_types", "risk_min", "risk_max",
            "requires_speculative_unlock", "buckets_json", "rebalance_cadence",
            "drawdown_alert_pct", "is_active", "created_at",
        };

        private static readonly string[] ColumnTypes =
        {
            "uuid", "character varying(50)", "character varying(100)", "text[]", "integer", "integer",
            "boolean", "jsonb", "character varying(20)",
            "double precision", "boolean", "timestamp with time zone",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var seededAt = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.Sql(
                @"DELETE FROM ""Portfolio"".""strategy_templates"";");

            migrationBuilder.InsertData(
                schema: "Portfolio",
                table: "strategy_templates",
                columns: Columns,
                columnTypes: ColumnTypes,
                values: new object[,]
                {
                    {
                        // Money that must still be there. Passive equity only, majority cash and bonds.
                        new Guid("a3b20001-0000-0000-0000-000000000001"),
                        "capital_preservation", "Capital Preservation",
                        new[] { "MediumTermGoal", "Retirement" }, 0, 29, false,
                        "[{\"sleeve\":\"core\",\"weight\":0.20,\"rules\":{\"assetClasses\":[\"equity\"],\"types\":[\"etf\"]}},{\"sleeve\":\"stability\",\"weight\":0.40,\"rules\":{\"assetClasses\":[\"cash_like\"]}},{\"sleeve\":\"stability\",\"weight\":0.25,\"rules\":{\"assetClasses\":[\"fixed_income\"]}},{\"sleeve\":\"stability\",\"weight\":0.15,\"rules\":{\"assetClasses\":[\"gold\"]}}]",
                        "semi_annual", 0.06, true, seededAt
                    },
                    {
                        // Long horizon, no engagement. Index core, gold as the devaluation hedge.
                        new Guid("a3b20001-0000-0000-0000-000000000002"),
                        "retirement_set_and_forget", "Retirement / Set-and-Forget",
                        new[] { "Retirement" }, 30, 100, false,
                        "[{\"sleeve\":\"core\",\"weight\":0.40,\"rules\":{\"assetClasses\":[\"equity\"],\"types\":[\"etf\"]}},{\"sleeve\":\"stability\",\"weight\":0.25,\"rules\":{\"assetClasses\":[\"gold\"]}},{\"sleeve\":\"stability\",\"weight\":0.20,\"rules\":{\"assetClasses\":[\"fixed_income\"]}},{\"sleeve\":\"stability\",\"weight\":0.15,\"rules\":{\"assetClasses\":[\"cash_like\"]}}]",
                        "semi_annual", 0.15, true, seededAt
                    },
                    {
                        // The mainstream builder: ranked stock picking with an index anchor beneath it.
                        new Guid("a3b20001-0000-0000-0000-000000000003"),
                        "balanced_growth", "Balanced Growth",
                        new[] { "LongTermWealth", "MediumTermGoal" }, 30, 69, false,
                        "[{\"sleeve\":\"core\",\"weight\":0.50,\"rules\":{\"types\":[\"stock\"]}},{\"sleeve\":\"core\",\"weight\":0.15,\"rules\":{\"assetClasses\":[\"equity\"],\"types\":[\"etf\"]}},{\"sleeve\":\"stability\",\"weight\":0.20,\"rules\":{\"assetClasses\":[\"gold\",\"fixed_income\"]}},{\"sleeve\":\"stability\",\"weight\":0.15,\"rules\":{\"assetClasses\":[\"cash_like\"]}}]",
                        "monthly", 0.12, true, seededAt
                    },
                    {
                        // Engaged and higher risk. Adds the dip-buying tactical sleeve.
                        new Guid("a3b20001-0000-0000-0000-000000000004"),
                        "active_growth", "Active Growth",
                        new[] { "LongTermWealth", "SpeculationLearning" }, 70, 100, false,
                        "[{\"sleeve\":\"core\",\"weight\":0.45,\"rules\":{\"types\":[\"stock\"]}},{\"sleeve\":\"tactical\",\"weight\":0.30,\"rules\":{\"types\":[\"stock\"]}},{\"sleeve\":\"core\",\"weight\":0.15,\"rules\":{\"assetClasses\":[\"equity\"],\"types\":[\"etf\"]}},{\"sleeve\":\"stability\",\"weight\":0.10,\"rules\":{\"assetClasses\":[\"cash_like\"]}}]",
                        "weekly", 0.2, true, seededAt
                    },
                    {
                        // Gated: needs experience AND capacity AND an explicit opt-in (RiskScoring v1).
                        new Guid("a3b20001-0000-0000-0000-000000000005"),
                        "opportunistic_speculative", "Opportunistic / Speculation & Learning",
                        new[] { "SpeculationLearning" }, 70, 100, true,
                        "[{\"sleeve\":\"core\",\"weight\":0.40,\"rules\":{\"types\":[\"stock\"]}},{\"sleeve\":\"tactical\",\"weight\":0.30,\"rules\":{\"types\":[\"stock\"]}},{\"sleeve\":\"speculative\",\"weight\":0.20,\"rules\":{\"types\":[\"stock\"]}},{\"sleeve\":\"stability\",\"weight\":0.10,\"rules\":{\"assetClasses\":[\"cash_like\"]}}]",
                        "weekly", 0.3, true, seededAt
                    },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes the five. The original four are restored by re-running the
            // AddStrategyTemplates migration, which owns their definition.
            migrationBuilder.Sql(
                @"DELETE FROM ""Portfolio"".""strategy_templates"";");
        }
    }
}
