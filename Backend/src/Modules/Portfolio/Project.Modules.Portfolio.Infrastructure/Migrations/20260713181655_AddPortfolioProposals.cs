using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "portfolio_proposals",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    template_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    template_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rebalance_cadence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    drawdown_alert_pct = table.Column<double>(type: "double precision", nullable: false),
                    risk_band = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    effective_risk = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    positions_json = table.Column<string>(type: "jsonb", nullable: false),
                    assumptions_json = table.Column<string>(type: "jsonb", nullable: false),
                    inputs_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portfolio_proposals", x => x.id);
                    table.ForeignKey(
                        name: "fk_portfolio_proposals_goals_goal_id",
                        column: x => x.goal_id,
                        principalSchema: "Portfolio",
                        principalTable: "goals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_proposals_goal_id_version",
                schema: "Portfolio",
                table: "portfolio_proposals",
                columns: new[] { "goal_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portfolio_proposals",
                schema: "Portfolio");
        }
    }
}
