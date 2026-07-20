using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Recommendations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyRunStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "Recommendations",
                table: "daily_runs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "status_changed_at",
                schema: "Recommendations",
                table: "daily_runs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "status_reason",
                schema: "Recommendations",
                table: "daily_runs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            // Grandfather clause: every run ingested before the § 6.2 kill switch
            // existed was served to users, so it is Published by definition.
            migrationBuilder.Sql("""
                UPDATE "Recommendations".daily_runs
                SET status = 'Published',
                    status_changed_at = created_at
                WHERE status = '';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_daily_runs_status_generated_at",
                schema: "Recommendations",
                table: "daily_runs",
                columns: new[] { "status", "generated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_daily_runs_status_generated_at",
                schema: "Recommendations",
                table: "daily_runs");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "Recommendations",
                table: "daily_runs");

            migrationBuilder.DropColumn(
                name: "status_changed_at",
                schema: "Recommendations",
                table: "daily_runs");

            migrationBuilder.DropColumn(
                name: "status_reason",
                schema: "Recommendations",
                table: "daily_runs");
        }
    }
}
