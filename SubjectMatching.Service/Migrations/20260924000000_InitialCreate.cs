using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SubjectMatching.Service.Data;

#nullable disable

namespace SubjectMatching.Service.Migrations;

[DbContext(typeof(SubjectMatchingDbContext))]
[Migration("20260924000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "internship_subjects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                ProblemStatement = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TypeStage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                RequiredSkills = table.Column<string>(type: "jsonb", nullable: false),
                PreferredSkills = table.Column<string>(type: "jsonb", nullable: false),
                EducationRequirements = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                ExperienceRequirements = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                AvailablePositions = table.Column<int>(type: "integer", nullable: false),
                FilledPositions = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_internship_subjects", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "candidate_profiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StagiaireId = table.Column<Guid>(type: "uuid", nullable: false),
                UtilisateurId = table.Column<long>(type: "bigint", nullable: false),
                CvFileHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Education = table.Column<string>(type: "text", nullable: true),
                Skills = table.Column<string>(type: "jsonb", nullable: false),
                Experience = table.Column<string>(type: "text", nullable: true),
                Projects = table.Column<string>(type: "jsonb", nullable: false),
                Languages = table.Column<string>(type: "jsonb", nullable: false),
                ParsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_candidate_profiles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "subject_matches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StagiaireId = table.Column<Guid>(type: "uuid", nullable: false),
                SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                CompatibilityScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                MatchedSkills = table.Column<string>(type: "jsonb", nullable: false),
                MissingSkills = table.Column<string>(type: "jsonb", nullable: false),
                Explanation = table.Column<string>(type: "text", nullable: false),
                GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_subject_matches", x => x.Id);
                table.ForeignKey(
                    name: "FK_subject_matches_internship_subjects_SubjectId",
                    column: x => x.SubjectId,
                    principalTable: "internship_subjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "subject_assignments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StagiaireId = table.Column<Guid>(type: "uuid", nullable: false),
                UtilisateurId = table.Column<long>(type: "bigint", nullable: false),
                SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ScoreAtProposal = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                ProposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_subject_assignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_subject_assignments_internship_subjects_SubjectId",
                    column: x => x.SubjectId,
                    principalTable: "internship_subjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "subject_change_requests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                StagiaireId = table.Column<Guid>(type: "uuid", nullable: false),
                UtilisateurId = table.Column<long>(type: "bigint", nullable: false),
                CurrentSubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                RequestedSubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                AdminComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_subject_change_requests", x => x.Id);
                table.ForeignKey(
                    name: "FK_subject_change_requests_internship_subjects_CurrentSubjectId",
                    column: x => x.CurrentSubjectId,
                    principalTable: "internship_subjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_subject_change_requests_internship_subjects_RequestedSubjectId",
                    column: x => x.RequestedSubjectId,
                    principalTable: "internship_subjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_subject_change_requests_subject_assignments_AssignmentId",
                    column: x => x.AssignmentId,
                    principalTable: "subject_assignments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_candidate_profiles_StagiaireId",
            table: "candidate_profiles",
            column: "StagiaireId");

        migrationBuilder.CreateIndex(
            name: "IX_candidate_profiles_UtilisateurId",
            table: "candidate_profiles",
            column: "UtilisateurId");

        migrationBuilder.CreateIndex(
            name: "IX_internship_subjects_Department",
            table: "internship_subjects",
            column: "Department");

        migrationBuilder.CreateIndex(
            name: "IX_internship_subjects_Status",
            table: "internship_subjects",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_subject_assignments_StagiaireId",
            table: "subject_assignments",
            column: "StagiaireId");

        migrationBuilder.CreateIndex(
            name: "IX_subject_assignments_SubjectId",
            table: "subject_assignments",
            column: "SubjectId");

        migrationBuilder.CreateIndex(
            name: "IX_subject_assignments_UtilisateurId",
            table: "subject_assignments",
            column: "UtilisateurId");

        migrationBuilder.CreateIndex(
            name: "IX_subject_change_requests_AssignmentId",
            table: "subject_change_requests",
            column: "AssignmentId");

        migrationBuilder.CreateIndex(
            name: "IX_subject_change_requests_CurrentSubjectId",
            table: "subject_change_requests",
            column: "CurrentSubjectId");

        migrationBuilder.CreateIndex(
            name: "IX_subject_change_requests_RequestedSubjectId",
            table: "subject_change_requests",
            column: "RequestedSubjectId");

        migrationBuilder.CreateIndex(
            name: "IX_subject_change_requests_StagiaireId",
            table: "subject_change_requests",
            column: "StagiaireId");

        migrationBuilder.CreateIndex(
            name: "IX_subject_change_requests_Status",
            table: "subject_change_requests",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_subject_change_requests_UtilisateurId",
            table: "subject_change_requests",
            column: "UtilisateurId");

        migrationBuilder.CreateIndex(
            name: "IX_subject_matches_StagiaireId",
            table: "subject_matches",
            column: "StagiaireId");

        migrationBuilder.CreateIndex(
            name: "IX_subject_matches_SubjectId",
            table: "subject_matches",
            column: "SubjectId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "subject_change_requests");
        migrationBuilder.DropTable(name: "subject_assignments");
        migrationBuilder.DropTable(name: "subject_matches");
        migrationBuilder.DropTable(name: "candidate_profiles");
        migrationBuilder.DropTable(name: "internship_subjects");
    }
}
