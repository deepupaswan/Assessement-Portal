using System;
using CandidateService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CandidateService.Infrastructure.Migrations
{
    [DbContext(typeof(CandidateDbContext))]
    [Migration("20260427173000_RestoreAssignmentLifecycleFields")]
    public partial class RestoreAssignmentLifecycleFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAtUtc",
                table: "CandidateAssessments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "CandidateAssessments",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledAtUtc",
                table: "CandidateAssessments");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "CandidateAssessments");
        }
    }
}
