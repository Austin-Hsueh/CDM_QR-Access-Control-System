using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class CourseUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 先移除現有的外鍵約束和索引
            migrationBuilder.Sql("ALTER TABLE tblStudentPermission DROP FOREIGN KEY IF EXISTS FK_tblStudentPermission_tblCourse_CourseId;");
            migrationBuilder.Sql("ALTER TABLE tblStudentPermission DROP FOREIGN KEY IF EXISTS FK_tblStudentPermission_tblUser_TeacherId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_tblStudentPermission_CourseId ON tblStudentPermission;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_tblStudentPermission_TeacherId ON tblStudentPermission;");

            // 2. 創建課程表
            migrationBuilder.CreateTable(
                name: "tblCourse",
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
                    table.PrimaryKey("PK_tblCourse", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 3. 插入一個默認課程
            migrationBuilder.InsertData(
                table: "tblCourse",
                columns: new[] { "Id", "Name", "IsEnable", "IsDelete", "ModifiedTime", "CreatedTime" },
                values: new object[] { 1, "Default Course", true, false, DateTime.Now, DateTime.Now });

            // 4. 更新 tblStudentPermission 表中的 CourseId 為默認值
            migrationBuilder.Sql("UPDATE tblStudentPermission SET CourseId = 1 WHERE CourseId = 0 OR CourseId IS NULL OR CourseId NOT IN (SELECT Id FROM tblCourse)");

            // 5. 創建索引
            migrationBuilder.CreateIndex(
                name: "IX_tblStudentPermission_CourseId",
                table: "tblStudentPermission",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_tblStudentPermission_TeacherId",
                table: "tblStudentPermission",
                column: "TeacherId");

            // 6. 添加外鍵約束
            migrationBuilder.AddForeignKey(
                name: "FK_tblStudentPermission_tblCourse_CourseId",
                table: "tblStudentPermission",
                column: "CourseId",
                principalTable: "tblCourse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tblStudentPermission_tblUser_TeacherId",
                table: "tblStudentPermission",
                column: "TeacherId",
                principalTable: "tblUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblStudentPermission_tblCourse_CourseId",
                table: "tblStudentPermission");

            migrationBuilder.DropForeignKey(
                name: "FK_tblStudentPermission_tblUser_TeacherId",
                table: "tblStudentPermission");

            migrationBuilder.DropTable(
                name: "tblCourse");

            migrationBuilder.DropIndex(
                name: "IX_tblStudentPermission_CourseId",
                table: "tblStudentPermission");

            migrationBuilder.DropIndex(
                name: "IX_tblStudentPermission_TeacherId",
                table: "tblStudentPermission");
        }
    }
} 