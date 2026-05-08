using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnswerService.Infrastructure.Migrations
{
    public partial class AddAnswerLookupIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Answers_AssessmentId",
                table: "Answers",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_CandidateId",
                table: "Answers",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_CandidateId_AssessmentId",
                table: "Answers",
                columns: new[] { "CandidateId", "AssessmentId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Answers_AssessmentId",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_CandidateId",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_CandidateId_AssessmentId",
                table: "Answers");
        }
    }
}
