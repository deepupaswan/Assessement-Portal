using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CandidateService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "AssessmentTitle",
                table: "CandidateAssessments");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "CandidateAssessments");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "CandidateAssessments");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "CandidateAssessments");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "CandidateAssessments");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "CandidateAssessments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CandidateAssessments");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "CandidateAssessments");

            migrationBuilder.DropColumn(
                name: "ViolationCount",
                table: "CandidateAssessments");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "CandidateAssessments",
                newName: "CompletedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CandidateAssessments",
                newName: "AssignedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Candidates",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Candidates");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "CandidateAssessments",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "AssignedAt",
                table: "CandidateAssessments",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "Candidates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "AssessmentTitle",
                table: "CandidateAssessments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "CandidateAssessments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxScore",
                table: "CandidateAssessments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "CandidateAssessments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "CandidateAssessments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "CandidateAssessments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CandidateAssessments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "CandidateAssessments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViolationCount",
                table: "CandidateAssessments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
