using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddAttendanceFee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblAttendanceFee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "Id")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AttendanceId = table.Column<int>(type: "int", nullable: false, comment: "簽到記錄Id"),
                    Hours = table.Column<decimal>(type: "decimal(65,30)", nullable: false, comment: "扣課時數"),
                    Amount = table.Column<int>(type: "int", nullable: false, comment: "單堂學費"),
                    AdjustmentAmount = table.Column<int>(type: "int", nullable: false, comment: "單堂增減金額"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "建立時間"),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "修改時間")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblAttendanceFee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblAttendanceFee_tblAttendance_AttendanceId",
                        column: x => x.AttendanceId,
                        principalTable: "tblAttendance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6764), new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6764) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6766), new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6767) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6768), new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6768) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6769), new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6770) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6749), new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6759) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6761), new DateTime(2025, 12, 8, 16, 20, 29, 512, DateTimeKind.Local).AddTicks(6762) });

            migrationBuilder.CreateIndex(
                name: "IX_tblAttendanceFee_AttendanceId",
                table: "tblAttendanceFee",
                column: "AttendanceId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblAttendanceFee");

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
        }
    }
}
