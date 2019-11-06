using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LightBlog.Infrastruct.Migrations.PostMigration
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "blog_hilo",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "comment_hilo",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "Post",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    AuthorId = table.Column<int>(nullable: false),
                    AuthorName = table.Column<string>(maxLength: 60, nullable: false),
                    Title = table.Column<string>(maxLength: 160, nullable: false),
                    EntryName = table.Column<string>(maxLength: 160, nullable: true),
                    Content = table.Column<string>(nullable: false),
                    Excerpt = table.Column<string>(maxLength: 2000, nullable: true),
                    Tags = table.Column<string>(maxLength: 500, nullable: true),
                    PostViews = table.Column<int>(nullable: false),
                    Published = table.Column<DateTime>(nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false),
                    IsPublished = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Post", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    CommentatorId = table.Column<int>(nullable: false),
                    CommentatorName = table.Column<string>(maxLength: 60, nullable: false),
                    PostId = table.Column<int>(nullable: false),
                    Content = table.Column<string>(maxLength: 1000, nullable: false),
                    CreatedTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comment_Post_PostId",
                        column: x => x.PostId,
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comment_PostId",
                table: "Comment",
                column: "PostId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropTable(
                name: "Post");

            migrationBuilder.DropSequence(
                name: "blog_hilo");

            migrationBuilder.DropSequence(
                name: "comment_hilo");
        }
    }
}
