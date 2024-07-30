using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddStudentPermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblStudentPermission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "權限項目Id")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false, comment: "權限項目所屬使用者([tblUser].[Id])"),
                    IsEnable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateFrom = table.Column<string>(type: "varchar(10)", nullable: false, comment: "權限日期起")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateTo = table.Column<string>(type: "varchar(10)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeFrom = table.Column<string>(type: "varchar(5)", nullable: false, comment: "權限時間起")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeTo = table.Column<string>(type: "varchar(5)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Days = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PermissionLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblStudentPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblStudentPermission_tblUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TblPermissionGroupTblStudentPermission",
                columns: table => new
                {
                    PermissionGroupsId = table.Column<int>(type: "int", nullable: false),
                    StudentPermissionsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblPermissionGroupTblStudentPermission", x => new { x.PermissionGroupsId, x.StudentPermissionsId });
                    table.ForeignKey(
                        name: "FK_TblPermissionGroupTblStudentPermission_tblPermissionGroup_Pe~",
                        column: x => x.PermissionGroupsId,
                        principalTable: "tblPermissionGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblPermissionGroupTblStudentPermission_tblStudentPermission_~",
                        column: x => x.StudentPermissionsId,
                        principalTable: "tblStudentPermission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4639), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4640) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4646), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4647) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4650), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4652) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4655), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4656) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4609), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4623) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4630), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4631) });

            migrationBuilder.CreateIndex(
                name: "IX_TblPermissionGroupTblStudentPermission_StudentPermissionsId",
                table: "TblPermissionGroupTblStudentPermission",
                column: "StudentPermissionsId");

            migrationBuilder.CreateIndex(
                name: "IX_tblStudentPermission_UserId",
                table: "tblStudentPermission",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TblPermissionGroupTblStudentPermission");

            migrationBuilder.DropTable(
                name: "tblStudentPermission");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5248), new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5249) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5257), new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5259) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5262), new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5263) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5266), new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5267) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5219), new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5231) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5239), new DateTime(2024, 7, 25, 22, 50, 27, 581, DateTimeKind.Local).AddTicks(5240) });
        }
    }
}
