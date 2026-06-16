using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Recommendations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserHoldings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_holdings",
                schema: "Recommendations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "text", nullable: false),
                    allocation_pct = table.Column<double>(type: "double precision", nullable: false),
                    run_generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_holdings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_holdings_user_id",
                schema: "Recommendations",
                table: "user_holdings",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_holdings",
                schema: "Recommendations");
        }
    }
}
