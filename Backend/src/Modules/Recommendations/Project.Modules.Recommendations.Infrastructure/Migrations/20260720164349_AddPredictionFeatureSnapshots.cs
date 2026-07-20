using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Recommendations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionFeatureSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "features_json",
                schema: "Recommendations",
                table: "stock_predictions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model_version",
                schema: "Recommendations",
                table: "stock_predictions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scaler_hash",
                schema: "Recommendations",
                table: "stock_predictions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_predictions_model_version",
                schema: "Recommendations",
                table: "stock_predictions",
                column: "model_version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stock_predictions_model_version",
                schema: "Recommendations",
                table: "stock_predictions");

            migrationBuilder.DropColumn(
                name: "features_json",
                schema: "Recommendations",
                table: "stock_predictions");

            migrationBuilder.DropColumn(
                name: "model_version",
                schema: "Recommendations",
                table: "stock_predictions");

            migrationBuilder.DropColumn(
                name: "scaler_hash",
                schema: "Recommendations",
                table: "stock_predictions");
        }
    }
}
