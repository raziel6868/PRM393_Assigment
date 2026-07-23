using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFSchool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    OriginalFileSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HasBlockingErrors = table.Column<bool>(type: "bit", nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    CreatedUserCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedUserCount = table.Column<int>(type: "int", nullable: false),
                    CreatedProfileCount = table.Column<int>(type: "int", nullable: false),
                    CreatedLinkCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAssignmentCount = table.Column<int>(type: "int", nullable: false),
                    CreatedEnrollmentCount = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    ValidatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    CommittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportBatches_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatchRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SheetName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SheetRowNumber = table.Column<int>(type: "int", nullable: false),
                    ReferenceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatchRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportBatchRows_ImportBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_CreatedByUserId",
                table: "ImportBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_OriginalFileSha256",
                table: "ImportBatches",
                column: "OriginalFileSha256");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_Status",
                table: "ImportBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatchRows_BatchId_SheetName_SheetRowNumber",
                table: "ImportBatchRows",
                columns: new[] { "BatchId", "SheetName", "SheetRowNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportBatchRows");

            migrationBuilder.DropTable(
                name: "ImportBatches");
        }
    }
}
