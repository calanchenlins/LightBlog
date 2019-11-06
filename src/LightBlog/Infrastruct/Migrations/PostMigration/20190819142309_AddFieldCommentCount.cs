using Microsoft.EntityFrameworkCore.Migrations;

namespace LightBlog.Infrastruct.Migrations.PostMigration
{
    public partial class AddFieldCommentCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommentCount",
                table: "Post",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentCount",
                table: "Post");
        }
    }
}
