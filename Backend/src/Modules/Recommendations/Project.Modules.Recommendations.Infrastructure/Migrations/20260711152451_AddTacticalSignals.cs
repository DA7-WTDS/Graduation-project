using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Recommendations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTacticalSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "pct_vs_sma50",
                schema: "Recommendations",
                table: "stock_predictions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "rsi14",
                schema: "Recommendations",
                table: "stock_predictions",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pct_vs_sma50",
                schema: "Recommendations",
                table: "stock_predictions");

            migrationBuilder.DropColumn(
                name: "rsi14",
                schema: "Recommendations",
                table: "stock_predictions");
        }
    }
}
