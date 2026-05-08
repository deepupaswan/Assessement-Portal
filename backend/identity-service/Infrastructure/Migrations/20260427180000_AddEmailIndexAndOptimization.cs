using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailIndexAndOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add unique index on Email column with case-insensitive collation
            // This enables efficient email lookups without client-side ToLower()
            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_CaseInsensitive",
                table: "Users",
                column: "Email",
                unique: true)
                .Annotation("SqlServer:Clustered", false)
                .Annotation("SqlServer:Collation", "SQL_Latin1_General_CP1_CI_AS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email_CaseInsensitive",
                table: "Users");
        }
    }
}
