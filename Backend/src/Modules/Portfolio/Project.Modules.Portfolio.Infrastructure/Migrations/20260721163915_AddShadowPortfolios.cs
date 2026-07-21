using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShadowPortfolios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shadow_portfolios",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    template_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    market = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    risk_band = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rebalance_cadence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    drawdown_alert_pct = table.Column<double>(type: "double precision", nullable: false),
                    notional = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cash_balance = table.Column<double>(type: "double precision", nullable: false),
                    last_nav = table.Column<double>(type: "double precision", nullable: false),
                    high_water_mark_nav = table.Column<double>(type: "double precision", nullable: false),
                    inception_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_valued_on = table.Column<DateOnly>(type: "date", nullable: true),
                    last_rebalanced_on = table.Column<DateOnly>(type: "date", nullable: true),
                    drawdown_alert_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shadow_portfolios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shadow_snapshots",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shadow_portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    nav = table.Column<double>(type: "double precision", nullable: false),
                    daily_return = table.Column<double>(type: "double precision", nullable: false),
                    rebalanced = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shadow_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shadow_position",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shadow_portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sleeve = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    shares = table.Column<double>(type: "double precision", nullable: false),
                    avg_cost = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shadow_position", x => x.id);
                    table.ForeignKey(
                        name: "fk_shadow_position_shadow_portfolios_shadow_portfolio_id",
                        column: x => x.shadow_portfolio_id,
                        principalSchema: "Portfolio",
                        principalTable: "shadow_portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shadow_portfolios_market_template_key",
                schema: "Portfolio",
                table: "shadow_portfolios",
                columns: new[] { "market", "template_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shadow_position_shadow_portfolio_id",
                schema: "Portfolio",
                table: "shadow_position",
                column: "shadow_portfolio_id");

            migrationBuilder.CreateIndex(
                name: "ix_shadow_snapshots_shadow_portfolio_id_date",
                schema: "Portfolio",
                table: "shadow_snapshots",
                columns: new[] { "shadow_portfolio_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shadow_position",
                schema: "Portfolio");

            migrationBuilder.DropTable(
                name: "shadow_snapshots",
                schema: "Portfolio");

            migrationBuilder.DropTable(
                name: "shadow_portfolios",
                schema: "Portfolio");
        }
    }
}
