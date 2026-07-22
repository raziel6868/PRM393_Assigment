using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFSchool.Infrastructure.Migrations;

public partial class AddAnnouncements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FeedPosts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Body = table.Column<string>(type: "nvarchar(max)", maxLength: 4000, nullable: false),
                Audience = table.Column<int>(type: "int", nullable: false),
                TargetClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AuthorDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                IsPublished = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", precision: 0, nullable: false),
                PublishedAtUtc = table.Column<DateTime>(type: "datetime2", precision: 0, nullable: true),
                ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeedPosts", x => x.Id);
                table.ForeignKey(
                    name: "FK_FeedPosts_ClassRooms_TargetClassId",
                    column: x => x.TargetClassId,
                    principalTable: "ClassRooms",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "AnnouncementDeliveries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FeedPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecipientDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Channel = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                SentAtUtc = table.Column<DateTime>(type: "datetime2", precision: 0, nullable: true),
                FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AnnouncementDeliveries", x => x.Id);
                table.ForeignKey(
                    name: "FK_AnnouncementDeliveries_FeedPosts_FeedPostId",
                    column: x => x.FeedPostId,
                    principalTable: "FeedPosts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AnnouncementReadStates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FeedPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReadAtUtc = table.Column<DateTime>(type: "datetime2", precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AnnouncementReadStates", x => x.Id);
                table.ForeignKey(
                    name: "FK_AnnouncementReadStates_FeedPosts_FeedPostId",
                    column: x => x.FeedPostId,
                    principalTable: "FeedPosts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FeedPosts_CreatedAtUtc",
            table: "FeedPosts",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_FeedPosts_IsPublished",
            table: "FeedPosts",
            column: "IsPublished");

        migrationBuilder.CreateIndex(
            name: "IX_FeedPosts_TargetClassId",
            table: "FeedPosts",
            column: "TargetClassId");

        migrationBuilder.CreateIndex(
            name: "IX_AnnouncementDeliveries_FeedPostId_RecipientUserId_Channel",
            table: "AnnouncementDeliveries",
            columns: new[] { "FeedPostId", "RecipientUserId", "Channel" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AnnouncementReadStates_FeedPostId_UserId",
            table: "AnnouncementReadStates",
            columns: new[] { "FeedPostId", "UserId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AnnouncementDeliveries");
        migrationBuilder.DropTable(name: "AnnouncementReadStates");
        migrationBuilder.DropTable(name: "FeedPosts");
    }
}
