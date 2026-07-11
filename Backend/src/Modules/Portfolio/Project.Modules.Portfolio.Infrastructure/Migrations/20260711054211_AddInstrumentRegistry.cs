using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "instruments",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    market = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    asset_class = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    suitable_for = table.Column<List<string>>(type: "text[]", nullable: false),
                    realized_vol1y = table.Column<double>(type: "double precision", nullable: true),
                    avg_daily_value_traded = table.Column<double>(type: "double precision", nullable: true),
                    last_close = table.Column<double>(type: "double precision", nullable: true),
                    stats_as_of = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instruments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_instruments_market_symbol",
                schema: "Portfolio",
                table: "instruments",
                columns: new[] { "market", "symbol" },
                unique: true);

            // Curated US sleeve instruments (§ 3.2 v1 templates). Equities
            // auto-register nightly from the pipeline universe; these anchor the
            // stability/US-ETF buckets and are never auto-created. EGX
            // counterparts (index ETF, gold, MM funds) are seeded when the
            // licensed data lands (TODO(EGX-DATA), § 0.1).
            var seededAt = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                schema: "Portfolio",
                table: "instruments",
                columns: new[] { "id", "market", "symbol", "type", "asset_class", "currency", "sector", "suitable_for", "is_active", "metadata_json", "created_at" },
                columnTypes: new[] { "uuid", "character varying(10)", "character varying(20)", "character varying(20)", "character varying(20)", "character varying(3)", "character varying(100)", "text[]", "boolean", "jsonb", "timestamp with time zone" },
                values: new object[,]
                {
                    { new Guid("a1b40000-0000-0000-0000-000000000001"), "us", "SPY", "Etf", "Equity", "USD", null, new[] { "core" }, true, "{\"index_tracked\":\"S&P 500\",\"expense_ratio\":0.0945}", seededAt },
                    { new Guid("a1b40000-0000-0000-0000-000000000002"), "us", "GLD", "Etf", "Gold", "USD", null, new[] { "stability" }, true, "{\"index_tracked\":\"Gold bullion\",\"expense_ratio\":0.40}", seededAt },
                    { new Guid("a1b40000-0000-0000-0000-000000000003"), "us", "AGG", "Etf", "FixedIncome", "USD", null, new[] { "stability" }, true, "{\"index_tracked\":\"Bloomberg US Aggregate Bond\",\"expense_ratio\":0.03}", seededAt },
                    { new Guid("a1b40000-0000-0000-0000-000000000004"), "us", "BIL", "Etf", "CashLike", "USD", null, new[] { "stability" }, true, "{\"index_tracked\":\"Bloomberg 1-3 Month T-Bill\",\"expense_ratio\":0.1356}", seededAt },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instruments",
                schema: "Portfolio");
        }
    }
}
