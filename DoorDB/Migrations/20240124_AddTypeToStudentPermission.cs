using Microsoft.EntityFrameworkCore.Migrations;

namespace DoorDB.Migrations
{
    public partial class AddTypeToStudentPermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 新增 Type 欄位
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "tblStudentPermission",
                nullable: false,
                defaultValue: 1);  // 預設值為 1 (上課)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 移除 Type 欄位
            migrationBuilder.DropColumn(
                name: "Type",
                table: "tblStudentPermission");
        }
    }
}