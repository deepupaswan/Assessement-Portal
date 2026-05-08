using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResultService.Infrastructure.Migrations
{
    public partial class AddResultLookupIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Results_CandidateId",
                table: "Results",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_AssessmentId",
                table: "Results",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_CandidateId_AssessmentId",
                table: "Results",
                columns: new[] { "CandidateId", "AssessmentId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Results_CandidateId",
                table: "Results");

            migrationBuilder.DropIndex(
                name: "IX_Results_AssessmentId",
                table: "Results");

            migrationBuilder.DropIndex(
                name: "IX_Results_CandidateId_AssessmentId",
                table: "Results");
        }
    }
}
