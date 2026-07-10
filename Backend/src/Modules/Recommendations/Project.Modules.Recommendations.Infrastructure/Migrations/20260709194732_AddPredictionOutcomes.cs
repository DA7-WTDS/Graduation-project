using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Recommendations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prediction_outcomes",
                schema: "Recommendations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_prediction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    run_generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    predicted_direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    predicted_change_pct = table.Column<double>(type: "double precision", nullable: false),
                    risk_level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    horizon_days = table.Column<int>(type: "integer", nullable: false),
                    baseline_close = table.Column<double>(type: "double precision", nullable: false),
                    realized_close = table.Column<double>(type: "double precision", nullable: false),
                    realized_return_pct = table.Column<double>(type: "double precision", nullable: false),
                    direction_hit = table.Column<bool>(type: "boolean", nullable: false),
                    scored_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prediction_outcomes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_prediction_outcomes_run_generated_at",
                schema: "Recommendations",
                table: "prediction_outcomes",
                column: "run_generated_at");

            migrationBuilder.CreateIndex(
                name: "ix_prediction_outcomes_stock_prediction_id",
                schema: "Recommendations",
                table: "prediction_outcomes",
                column: "stock_prediction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prediction_outcomes",
                schema: "Recommendations");
        }
    }
}
