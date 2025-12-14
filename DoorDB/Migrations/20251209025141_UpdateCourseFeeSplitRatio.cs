using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class UpdateCourseFeeSplitRatio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "SplitRatio",
                table: "tblCourseFee",
                type: "decimal(65,30)",
                nullable: false,
                comment: "預設拆帳比例",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "預設拆帳比例");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3203), new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3203) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3205), new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3205) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3206), new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3207) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3208), new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3208) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3185), new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3196) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3198), new DateTime(2025, 12, 9, 10, 51, 41, 360, DateTimeKind.Local).AddTicks(3199) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SplitRatio",
                table: "tblCourseFee",
                type: "int",
                nullable: false,
                comment: "預設拆帳比例",
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)",
                oldComment: "預設拆帳比例");

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
        }
    }
}
