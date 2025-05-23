using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class Course : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "tblStudentPermission",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "權限項目所屬使用者([tblCourse].[Id])");

            migrationBuilder.AddColumn<int>(
                name: "TeacherId",
                table: "tblStudentPermission",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "權限項目所屬老師([tblUser].[Id])");

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

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9463), new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9464) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9466), new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9466) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9467), new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9468) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9468), new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9469) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9446), new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9458) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9461), new DateTime(2025, 5, 1, 22, 14, 15, 182, DateTimeKind.Local).AddTicks(9461) });

            migrationBuilder.CreateIndex(
                name: "IX_tblStudentPermission_CourseId",
                table: "tblStudentPermission",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_tblStudentPermission_TeacherId",
                table: "tblStudentPermission",
                column: "TeacherId");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblCourse");

            migrationBuilder.DropIndex(
                name: "IX_tblStudentPermission_CourseId",
                table: "tblStudentPermission");

            migrationBuilder.DropIndex(
                name: "IX_tblStudentPermission_TeacherId",
                table: "tblStudentPermission");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "tblStudentPermission");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "tblStudentPermission");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7432), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7433) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7435), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7435) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7437), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7437) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7438), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7438) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7418), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7427) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7430), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7430) });
        }
    }
}
