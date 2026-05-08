using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CandidateService.Infrastructure.Migrations
{
    public partial class AddCandidateAssessmentIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessments_AssessmentId",
                table: "CandidateAssessments",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessments_CandidateId_AssessmentId",
                table: "CandidateAssessments",
                columns: new[] { "CandidateId", "AssessmentId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateAssessments_AssessmentId",
                table: "CandidateAssessments");

            migrationBuilder.DropIndex(
                name: "IX_CandidateAssessments_CandidateId_AssessmentId",
                table: "CandidateAssessments");
        }
    }
}
