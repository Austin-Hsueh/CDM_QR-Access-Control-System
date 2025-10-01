using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddScheduleTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblSchedule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentPermissionId = table.Column<int>(type: "int", nullable: false, comment: "學生權限Id ([tblStudentPermission].[Id])"),
                    ClassroomId = table.Column<int>(type: "int", nullable: false, comment: "教室Id ([tblClassroom].[Id])"),
                    ScheduleDate = table.Column<string>(type: "varchar(10)", nullable: false, comment: "課程日期")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<string>(type: "varchar(5)", nullable: false, comment: "課程開始時間")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EndTime = table.Column<string>(type: "varchar(5)", nullable: false, comment: "課程結束時間")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CourseMode = table.Column<int>(type: "int", nullable: false, comment: "課程模式 1=現場 2=視訊"),
                    ScheduleMode = table.Column<int>(type: "int", nullable: false, comment: "排課模式 1=每週固定 2=每兩週固定 3=單次課程"),
                    QRCodeContent = table.Column<string>(type: "longtext", nullable: true, comment: "QR Code 內容")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false, comment: "課程狀態 1=正常 2=取消 3=延期"),
                    Remark = table.Column<string>(type: "longtext", nullable: true, comment: "備註")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSchedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblSchedule_tblClassroom_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "tblClassroom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblSchedule_tblStudentPermission_StudentPermissionId",
                        column: x => x.StudentPermissionId,
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
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(632), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(633) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(635), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(636) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(637), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(637) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(638), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(638) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(618), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(626) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(629), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(629) });

            migrationBuilder.CreateIndex(
                name: "IX_tblSchedule_ClassroomId",
                table: "tblSchedule",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_tblSchedule_StudentPermissionId",
                table: "tblSchedule",
                column: "StudentPermissionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblSchedule");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6729), new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6729) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6731), new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6731) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6732), new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6733) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6733), new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6734) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6713), new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6722) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6725), new DateTime(2025, 9, 29, 14, 52, 59, 70, DateTimeKind.Local).AddTicks(6725) });
        }
    }
}
