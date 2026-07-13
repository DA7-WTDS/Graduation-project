using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalPortfolios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "goal_portfolios",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    drawdown_threshold = table.Column<double>(type: "double precision", nullable: false),
                    high_water_mark_nav = table.Column<double>(type: "double precision", nullable: false),
                    last_nav = table.Column<double>(type: "double precision", nullable: false),
                    last_valued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    drawdown_alert_active = table.Column<bool>(type: "boolean", nullable: false),
                    drift_alert_active = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    inception_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goal_portfolios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_holding",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sleeve = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_weight = table.Column<double>(type: "double precision", nullable: false),
                    entry_price = table.Column<double>(type: "double precision", nullable: false),
                    shares = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portfolio_holding", x => x.id);
                    table.ForeignKey(
                        name: "fk_portfolio_holding_goal_portfolios_goal_portfolio_id",
                        column: x => x.goal_portfolio_id,
                        principalSchema: "Portfolio",
                        principalTable: "goal_portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_goal_portfolios_goal_id_status",
                schema: "Portfolio",
                table: "goal_portfolios",
                columns: new[] { "goal_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_holding_goal_portfolio_id",
                schema: "Portfolio",
                table: "portfolio_holding",
                column: "goal_portfolio_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portfolio_holding",
                schema: "Portfolio");

            migrationBuilder.DropTable(
                name: "goal_portfolios",
                schema: "Portfolio");
        }
    }
}
