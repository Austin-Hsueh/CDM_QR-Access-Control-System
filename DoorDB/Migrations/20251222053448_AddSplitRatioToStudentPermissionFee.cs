using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddSplitRatioToStudentPermissionFee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CourseSplitRatio",
                table: "tblStudentPermissionFee",
                type: "decimal(65,30)",
                nullable: true,
                comment: "課程拆帳比");

            migrationBuilder.AddColumn<decimal>(
                name: "TeacherSplitRatio",
                table: "tblStudentPermissionFee",
                type: "decimal(65,30)",
                nullable: true,
                comment: "老師拆帳比");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7832), new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7832) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7835), new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7836) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7837), new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7838) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7839), new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7840) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7808), new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7823) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7826), new DateTime(2025, 12, 22, 13, 34, 47, 756, DateTimeKind.Local).AddTicks(7827) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseSplitRatio",
                table: "tblStudentPermissionFee");

            migrationBuilder.DropColumn(
                name: "TeacherSplitRatio",
                table: "tblStudentPermissionFee");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9986), new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9986) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9988), new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9989) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9990), new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9991) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9992), new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9992) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9967), new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9980) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9982), new DateTime(2025, 12, 17, 9, 47, 11, 144, DateTimeKind.Local).AddTicks(9983) });
        }
    }
}
