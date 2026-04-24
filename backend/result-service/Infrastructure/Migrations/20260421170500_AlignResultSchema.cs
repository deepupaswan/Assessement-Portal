using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ResultService.Infrastructure.Persistence;

#nullable disable

namespace ResultService.Infrastructure.Migrations
{
    [DbContext(typeof(ResultDbContext))]
    [Migration("20260421170500_AlignResultSchema")]
    public partial class AlignResultSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "Results",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CalculatedAt",
                table: "Results",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(name: "MaxScore", table: "Results", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<decimal>(name: "Percentage", table: "Results", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>(name: "Status", table: "Results", type: "nvarchar(max)", nullable: false, defaultValue: "Pending");
            migrationBuilder.AddColumn<int>(name: "TotalQuestions", table: "Results", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "CorrectAnswers", table: "Results", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "WrongAnswers", table: "Results", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "SkippedQuestions", table: "Results", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<DateTime>(name: "StartedAt", table: "Results", type: "datetime2", nullable: false, defaultValue: DateTime.UnixEpoch);
            migrationBuilder.AddColumn<DateTime>(name: "CompletedAt", table: "Results", type: "datetime2", nullable: false, defaultValue: DateTime.UnixEpoch);
            migrationBuilder.AddColumn<DateTime>(name: "EvaluatedAt", table: "Results", type: "datetime2", nullable: false, defaultValue: DateTime.UnixEpoch);
            migrationBuilder.AddColumn<DateTime>(name: "PublishedAt", table: "Results", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Remarks", table: "Results", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsPassed", table: "Results", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<decimal>(name: "PassingPercentage", table: "Results", type: "decimal(18,2)", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CompletedAt", table: "Results");
            migrationBuilder.DropColumn(name: "CorrectAnswers", table: "Results");
            migrationBuilder.DropColumn(name: "EvaluatedAt", table: "Results");
            migrationBuilder.DropColumn(name: "IsPassed", table: "Results");
            migrationBuilder.DropColumn(name: "MaxScore", table: "Results");
            migrationBuilder.DropColumn(name: "PassingPercentage", table: "Results");
            migrationBuilder.DropColumn(name: "Percentage", table: "Results");
            migrationBuilder.DropColumn(name: "PublishedAt", table: "Results");
            migrationBuilder.DropColumn(name: "Remarks", table: "Results");
            migrationBuilder.DropColumn(name: "SkippedQuestions", table: "Results");
            migrationBuilder.DropColumn(name: "StartedAt", table: "Results");
            migrationBuilder.DropColumn(name: "Status", table: "Results");
            migrationBuilder.DropColumn(name: "TotalQuestions", table: "Results");
            migrationBuilder.DropColumn(name: "WrongAnswers", table: "Results");

            migrationBuilder.AlterColumn<double>(
                name: "Score",
                table: "Results",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CalculatedAt",
                table: "Results",
                type: "datetime2",
                nullable: false,
                defaultValue: DateTime.UnixEpoch,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
