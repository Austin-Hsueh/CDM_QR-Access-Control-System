using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class CourseType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseTypeId",
                table: "tblCourse",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "tblCourseType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCourseType", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblCourse_tblCourseType_CourseTypeId",
                table: "tblCourse");

            migrationBuilder.DropTable(
                name: "tblCourseType");

            migrationBuilder.DropIndex(
                name: "IX_tblCourse_CourseTypeId",
                table: "tblCourse");

            migrationBuilder.DropColumn(
                name: "CourseTypeId",
                table: "tblCourse");
        }
    }
}
