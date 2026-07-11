using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Modules.Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalsAndInvestorProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "goals",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    horizon_years = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "questionnaire_responses",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answers_json = table.Column<string>(type: "jsonb", nullable: false),
                    scoring_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_questionnaire_responses", x => x.id);
                    table.ForeignKey(
                        name: "fk_questionnaire_responses_goals_goal_id",
                        column: x => x.goal_id,
                        principalSchema: "Portfolio",
                        principalTable: "goals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "investor_profiles",
                schema: "Portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    questionnaire_response_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    tolerance = table.Column<int>(type: "integer", nullable: false),
                    effective_risk = table.Column<int>(type: "integer", nullable: false),
                    risk_band = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    engagement = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    usd_comfort = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    speculative_unlocked = table.Column<bool>(type: "boolean", nullable: false),
                    scoring_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_investor_profiles_goals_goal_id",
                        column: x => x.goal_id,
                        principalSchema: "Portfolio",
                        principalTable: "goals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_investor_profiles_questionnaire_responses_questionnaire_res",
                        column: x => x.questionnaire_response_id,
                        principalSchema: "Portfolio",
                        principalTable: "questionnaire_responses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_goals_user_id",
                schema: "Portfolio",
                table: "goals",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_investor_profiles_goal_id_version",
                schema: "Portfolio",
                table: "investor_profiles",
                columns: new[] { "goal_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investor_profiles_questionnaire_response_id",
                schema: "Portfolio",
                table: "investor_profiles",
                column: "questionnaire_response_id");

            migrationBuilder.CreateIndex(
                name: "ix_questionnaire_responses_goal_id",
                schema: "Portfolio",
                table: "questionnaire_responses",
                column: "goal_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "investor_profiles",
                schema: "Portfolio");

            migrationBuilder.DropTable(
                name: "questionnaire_responses",
                schema: "Portfolio");

            migrationBuilder.DropTable(
                name: "goals",
                schema: "Portfolio");
        }
    }
}
