using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFSchool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationshipConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ParentStudentLinks",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ParentStudentLinks_ParentProfileId_IsActive",
                table: "ParentStudentLinks",
                columns: new[] { "ParentProfileId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParentStudentLinks_ParentProfileId_IsActive",
                table: "ParentStudentLinks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ParentStudentLinks");
        }
    }
}
