using Microsoft.EntityFrameworkCore.Migrations;

namespace KaneBlake.STS.Identity.Infrastruct.Migrations.UserMigration
{
    public partial class UserDbContext_AddField_Salt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Salt",
                table: "User",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Salt",
                table: "User");
        }
    }
}
