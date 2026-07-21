using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFSchool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceTemporaryPasswordState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_TemporaryPasswordState",
                table: "Users",
                sql: "([MustChangePassword] = 1 AND [TemporaryPasswordExpiresAtUtc] IS NOT NULL) OR ([MustChangePassword] = 0 AND [TemporaryPasswordExpiresAtUtc] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_TemporaryPasswordState",
                table: "Users");
        }
    }
}
