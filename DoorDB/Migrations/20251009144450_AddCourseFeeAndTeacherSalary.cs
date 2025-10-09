using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddCourseFeeAndTeacherSalary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblCourseFee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseTypeId = table.Column<int>(type: "int", nullable: false),
                    FeeName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FeeAmount = table.Column<int>(type: "int", nullable: false),
                    SplitRatio = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    LessonCount = table.Column<int>(type: "int", nullable: false),
                    IsStudentAbsenceNotDeduct = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsCountTransaction = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsArchived = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCourseFee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblCourseFee_tblCourseType_CourseTypeId",
                        column: x => x.CourseTypeId,
                        principalTable: "tblCourseType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblTeacherSalaryDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    CourseFeeId = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    BaseSplitAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    FlexibleSplitAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Bonus = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Deduction = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Discount = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FlexiblePoints = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTeacherSalaryDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblTeacherSalaryDetail_tblCourseFee_CourseFeeId",
                        column: x => x.CourseFeeId,
                        principalTable: "tblCourseFee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblTeacherSalaryDetail_tblSchedule_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "tblSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblTeacherSalaryDetail_tblUser_StudentId",
                        column: x => x.StudentId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblTeacherSalaryDetail_tblUser_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3551), new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3551) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3553), new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3554) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3555), new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3555) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3556), new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3556) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3533), new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3544) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3547), new DateTime(2025, 10, 9, 22, 44, 50, 369, DateTimeKind.Local).AddTicks(3547) });

            migrationBuilder.CreateIndex(
                name: "IX_tblCourseFee_CourseTypeId",
                table: "tblCourseFee",
                column: "CourseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTeacherSalaryDetail_CourseFeeId",
                table: "tblTeacherSalaryDetail",
                column: "CourseFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTeacherSalaryDetail_ScheduleId",
                table: "tblTeacherSalaryDetail",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTeacherSalaryDetail_StudentId",
                table: "tblTeacherSalaryDetail",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTeacherSalaryDetail_TeacherId",
                table: "tblTeacherSalaryDetail",
                column: "TeacherId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblTeacherSalaryDetail");

            migrationBuilder.DropTable(
                name: "tblCourseFee");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2440), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2442) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2444), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2444) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2445), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2446) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2448), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2449) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2422), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2434) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2437), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2438) });
        }
    }
}
