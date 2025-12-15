using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddTotalAmountAndSplitRatioFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalAmount",
                table: "tblStudentPermissionFee",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "總金額");

            migrationBuilder.AddColumn<decimal>(
                name: "SourceHoursTotalAmount",
                table: "tblAttendanceFee",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m,
                comment: "原始時數總金額");

            migrationBuilder.AddColumn<decimal>(
                name: "UseSplitRatio",
                table: "tblAttendanceFee",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m,
                comment: "使用的拆帳比");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3827), new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3827) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3830), new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3831) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3832), new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3833) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3834), new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3835) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3800), new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3818) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3821), new DateTime(2025, 12, 15, 16, 27, 42, 217, DateTimeKind.Local).AddTicks(3822) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "tblStudentPermissionFee");

            migrationBuilder.DropColumn(
                name: "SourceHoursTotalAmount",
                table: "tblAttendanceFee");

            migrationBuilder.DropColumn(
                name: "UseSplitRatio",
                table: "tblAttendanceFee");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7536), new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7536) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7538), new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7538) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7539), new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7540) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7541), new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7541) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7514), new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7526) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7528), new DateTime(2025, 12, 12, 11, 41, 47, 346, DateTimeKind.Local).AddTicks(7529) });
        }
    }
}
