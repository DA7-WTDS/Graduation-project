using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Recommendations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulatedRunsAndMarket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "market",
                schema: "Recommendations",
                table: "daily_runs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "us");

            migrationBuilder.CreateIndex(
                name: "ix_daily_runs_market_generated_at_status",
                schema: "Recommendations",
                table: "daily_runs",
                columns: new[] { "market", "generated_at", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_daily_runs_market_generated_at_status",
                schema: "Recommendations",
                table: "daily_runs");

            migrationBuilder.DropColumn(
                name: "market",
                schema: "Recommendations",
                table: "daily_runs");
        }
    }
}
