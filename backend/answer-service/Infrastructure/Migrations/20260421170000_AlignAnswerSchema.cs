using System;
using AnswerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnswerService.Infrastructure.Migrations
{
    [DbContext(typeof(AnswerDbContext))]
    [Migration("20260421170000_AlignAnswerSchema")]
    public partial class AlignAnswerSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "Response",
                table: "Answers");

            migrationBuilder.AddColumn<Guid>(
                name: "QuestionId",
                table: "Answers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "SelectedOptionId",
                table: "Answers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionText",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AnswerText",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "Answers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PointsObtained",
                table: "Answers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalPoints",
                table: "Answers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "Answers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradingNotes",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AnswerText", table: "Answers");
            migrationBuilder.DropColumn(name: "GradedAt", table: "Answers");
            migrationBuilder.DropColumn(name: "GradingNotes", table: "Answers");
            migrationBuilder.DropColumn(name: "IsCorrect", table: "Answers");
            migrationBuilder.DropColumn(name: "PointsObtained", table: "Answers");
            migrationBuilder.DropColumn(name: "QuestionId", table: "Answers");
            migrationBuilder.DropColumn(name: "QuestionText", table: "Answers");
            migrationBuilder.DropColumn(name: "SelectedOptionId", table: "Answers");
            migrationBuilder.DropColumn(name: "TotalPoints", table: "Answers");

            migrationBuilder.AddColumn<int>(
                name: "QuestionId",
                table: "Answers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Response",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
