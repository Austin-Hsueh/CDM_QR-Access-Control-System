using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddFeeSeries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeacherSettlementId",
                table: "tblUser",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CourseId",
                table: "tblStudentPermission",
                type: "int",
                nullable: true,
                comment: "權限項目所屬使用者([tblCourse].[Id])",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "權限項目所屬使用者([tblCourse].[Id])");

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "tblPayment",
                type: "longtext",
                nullable: true,
                comment: "結帳單號")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblCourseFee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "課程費用Id")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseId = table.Column<int>(type: "int", nullable: false, comment: "課程Id"),
                    Category = table.Column<string>(type: "longtext", nullable: true, comment: "類別 (個別班、團體班、不限)")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, comment: "排序"),
                    FeeCode = table.Column<string>(type: "longtext", nullable: false, comment: "課程費用編號")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<int>(type: "int", nullable: false, comment: "課程費用"),
                    MaterialFee = table.Column<int>(type: "int", nullable: false, comment: "預設教材費"),
                    Hours = table.Column<decimal>(type: "decimal(65,30)", nullable: false, comment: "繳費時數"),
                    SplitRatio = table.Column<int>(type: "int", nullable: false, comment: "預設拆帳比例"),
                    OpenCourseAmount = table.Column<int>(type: "int", nullable: false, comment: "開放課程費用"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "建立時間"),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "修改時間")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCourseFee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblCourseFee_tblCourse_CourseId",
                        column: x => x.CourseId,
                        principalTable: "tblCourse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblStudentPermissionFee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "Id")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentPermissionId = table.Column<int>(type: "int", nullable: false, comment: "學生權限Id"),
                    PaymentDate = table.Column<DateTime>(type: "datetime(6)", nullable: true, comment: "繳款日期"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "建立時間"),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "修改時間")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblStudentPermissionFee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblStudentPermissionFee_tblStudentPermission_StudentPermissi~",
                        column: x => x.StudentPermissionId,
                        principalTable: "tblStudentPermission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblTeacherSettlement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "Id")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TeacherId = table.Column<int>(type: "int", nullable: false, comment: "老師Id"),
                    SplitRatio = table.Column<decimal>(type: "decimal(65,30)", nullable: false, comment: "老師拆帳比例"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "建立時間"),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "修改時間")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTeacherSettlement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblTeacherSettlement_tblUser_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7715), new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7716) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7718), new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7719) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7720), new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7721) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7722), new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7723) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7692), new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7707) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7710), new DateTime(2025, 12, 8, 10, 53, 31, 848, DateTimeKind.Local).AddTicks(7710) });

            migrationBuilder.CreateIndex(
                name: "IX_tblUser_TeacherSettlementId",
                table: "tblUser",
                column: "TeacherSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_tblCourseFee_CourseId",
                table: "tblCourseFee",
                column: "CourseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblStudentPermissionFee_StudentPermissionId",
                table: "tblStudentPermissionFee",
                column: "StudentPermissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblTeacherSettlement_TeacherId",
                table: "tblTeacherSettlement",
                column: "TeacherId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tblUser_tblTeacherSettlement_TeacherSettlementId",
                table: "tblUser",
                column: "TeacherSettlementId",
                principalTable: "tblTeacherSettlement",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblUser_tblTeacherSettlement_TeacherSettlementId",
                table: "tblUser");

            migrationBuilder.DropTable(
                name: "tblCourseFee");

            migrationBuilder.DropTable(
                name: "tblStudentPermissionFee");

            migrationBuilder.DropTable(
                name: "tblTeacherSettlement");

            migrationBuilder.DropIndex(
                name: "IX_tblUser_TeacherSettlementId",
                table: "tblUser");

            migrationBuilder.DropColumn(
                name: "TeacherSettlementId",
                table: "tblUser");

            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "tblPayment");

            migrationBuilder.AlterColumn<int>(
                name: "CourseId",
                table: "tblStudentPermission",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "權限項目所屬使用者([tblCourse].[Id])",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComment: "權限項目所屬使用者([tblCourse].[Id])");

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
