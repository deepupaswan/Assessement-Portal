using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssessmentService.Infrastructure.Migrations
{
    public partial class AddQuestionOrderingIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Questions_AssessmentId_Sequence",
                table: "Questions",
                columns: new[] { "AssessmentId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_QuestionId_Sequence",
                table: "QuestionOptions",
                columns: new[] { "QuestionId", "Sequence" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_AssessmentId_Sequence",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_QuestionOptions_QuestionId_Sequence",
                table: "QuestionOptions");
        }
    }
}
