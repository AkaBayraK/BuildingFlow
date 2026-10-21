using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <summary>Anketler: surveys, questions, options, participations and (optionally anonymous) responses; additive only.</summary>
    public partial class Surveys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "surveys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    audience = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    response_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    response_count = table.Column<int>(type: "integer", nullable: false),
                    eligible_count = table.Column<int>(type: "integer", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_surveys", x => x.id);
                    table.UniqueConstraint("AK_surveys_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_surveys_status", "(status = 'DRAFT' AND published_at IS NULL AND closed_at IS NULL AND response_count = 0) OR (status = 'PUBLISHED' AND published_at IS NOT NULL AND closed_at IS NULL) OR (status = 'CLOSED' AND published_at IS NOT NULL AND closed_at IS NOT NULL AND eligible_count IS NOT NULL)");
                    table.CheckConstraint("ck_surveys_valid", "category IN ('DUES','SERVICE','SECURITY','SOCIAL','PARKING','ENVIRONMENT','GENERAL') AND audience IN ('ALL_RESIDENTS','OWNERS','TENANTS') AND response_mode IN ('PER_UNIT','PER_ACCOUNT') AND length(trim(title)) > 0 AND start_date BETWEEN DATE '2000-01-01' AND DATE '2100-12-31' AND end_date BETWEEN start_date AND DATE '2100-12-31' AND response_count >= 0 AND (eligible_count IS NULL OR eligible_count >= 0)");
                    table.ForeignKey(
                        name: "FK_surveys_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_participations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ballot_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    respondent_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_participations", x => x.id);
                    table.CheckConstraint("ck_survey_participations_valid", "role IN ('OWNER','TENANT','RESIDENT') AND (ballot_key LIKE 'UNIT:%' OR ballot_key LIKE 'ACCOUNT:%')");
                    table.ForeignKey(
                        name: "FK_survey_participations_surveys_property_id_survey_id",
                        columns: x => new { x.property_id, x.survey_id },
                        principalTable: "surveys",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_participations_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_questions", x => x.id);
                    table.UniqueConstraint("AK_survey_questions_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_survey_questions_valid", "type IN ('SINGLE_CHOICE','MULTIPLE_CHOICE','RATING','TEXT') AND position BETWEEN 1 AND 30 AND length(trim(text)) > 0");
                    table.ForeignKey(
                        name: "FK_survey_questions_surveys_property_id_survey_id",
                        columns: x => new { x.property_id, x.survey_id },
                        principalTable: "surveys",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_on = table.Column<DateOnly>(type: "date", nullable: false),
                    is_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_responses", x => x.id);
                    table.UniqueConstraint("AK_survey_responses_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_survey_responses_anonymous", "(is_anonymous AND participation_id IS NULL AND unit_id IS NULL AND created_by = 'anonymous' AND modified_by = 'anonymous') OR (NOT is_anonymous AND participation_id IS NOT NULL AND unit_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_survey_responses_survey_participations_participation_id",
                        column: x => x.participation_id,
                        principalTable: "survey_participations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_responses_surveys_property_id_survey_id",
                        columns: x => new { x.property_id, x.survey_id },
                        principalTable: "surveys",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_options", x => x.id);
                    table.UniqueConstraint("AK_survey_options_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_survey_options_valid", "position BETWEEN 1 AND 10 AND length(trim(text)) > 0");
                    table.ForeignKey(
                        name: "FK_survey_options_survey_questions_property_id_question_id",
                        columns: x => new { x.property_id, x.question_id },
                        principalTable: "survey_questions",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_options_surveys_property_id_survey_id",
                        columns: x => new { x.property_id, x.survey_id },
                        principalTable: "surveys",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_answers", x => x.id);
                    table.CheckConstraint("ck_survey_answers_anonymous", "NOT is_anonymous OR (created_by = 'anonymous' AND modified_by = 'anonymous')");
                    table.CheckConstraint("ck_survey_answers_value", "num_nonnulls(option_id, rating, text) = 1 AND (rating IS NULL OR rating BETWEEN 1 AND 5) AND (text IS NULL OR length(trim(text)) > 0)");
                    table.ForeignKey(
                        name: "FK_survey_answers_survey_options_property_id_option_id",
                        columns: x => new { x.property_id, x.option_id },
                        principalTable: "survey_options",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_answers_survey_questions_property_id_question_id",
                        columns: x => new { x.property_id, x.question_id },
                        principalTable: "survey_questions",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_answers_survey_responses_property_id_response_id",
                        columns: x => new { x.property_id, x.response_id },
                        principalTable: "survey_responses",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_property_id_option_id",
                table: "survey_answers",
                columns: new[] { "property_id", "option_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_property_id_question_id",
                table: "survey_answers",
                columns: new[] { "property_id", "question_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_property_id_response_id",
                table: "survey_answers",
                columns: new[] { "property_id", "response_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_survey_id_question_id",
                table: "survey_answers",
                columns: new[] { "survey_id", "question_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_options_property_id_question_id",
                table: "survey_options",
                columns: new[] { "property_id", "question_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_options_property_id_survey_id",
                table: "survey_options",
                columns: new[] { "property_id", "survey_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_options_question_id_position",
                table: "survey_options",
                columns: new[] { "question_id", "position" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_survey_participations_property_id_survey_id",
                table: "survey_participations",
                columns: new[] { "property_id", "survey_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_participations_property_id_unit_id",
                table: "survey_participations",
                columns: new[] { "property_id", "unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_participations_survey_id_ballot_key",
                table: "survey_participations",
                columns: new[] { "survey_id", "ballot_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_participations_survey_id_respondent_user_id",
                table: "survey_participations",
                columns: new[] { "survey_id", "respondent_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_questions_property_id_survey_id",
                table: "survey_questions",
                columns: new[] { "property_id", "survey_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_questions_survey_id_position",
                table: "survey_questions",
                columns: new[] { "survey_id", "position" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_participation_id",
                table: "survey_responses",
                column: "participation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_property_id_survey_id",
                table: "survey_responses",
                columns: new[] { "property_id", "survey_id" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_survey_id_submitted_on",
                table: "survey_responses",
                columns: new[] { "survey_id", "submitted_on" });

            migrationBuilder.CreateIndex(
                name: "IX_surveys_property_id_start_date",
                table: "surveys",
                columns: new[] { "property_id", "start_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Surveys and residents' answers are user data; Down refuses to discard them instead of silently losing history.
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM surveys) THEN RAISE EXCEPTION 'Survey history cannot be discarded'; END IF; END $$;");
            migrationBuilder.DropTable(
                name: "survey_answers");

            migrationBuilder.DropTable(
                name: "survey_options");

            migrationBuilder.DropTable(
                name: "survey_responses");

            migrationBuilder.DropTable(
                name: "survey_questions");

            migrationBuilder.DropTable(
                name: "survey_participations");

            migrationBuilder.DropTable(
                name: "surveys");
        }
    }
}
