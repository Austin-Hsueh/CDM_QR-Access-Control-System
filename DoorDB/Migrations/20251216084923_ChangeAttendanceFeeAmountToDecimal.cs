using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class ChangeAttendanceFeeAmountToDecimal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "tblAttendanceFee",
                type: "decimal(65,30)",
                nullable: false,
                comment: "單堂學費",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "單堂學費");

            migrationBuilder.AlterColumn<decimal>(
                name: "AdjustmentAmount",
                table: "tblAttendanceFee",
                type: "decimal(65,30)",
                nullable: false,
                comment: "單堂增減金額",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "單堂增減金額");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1793), new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1793) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1795), new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1795) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1796), new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1797) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1798), new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1798) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1774), new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1786) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1788), new DateTime(2025, 12, 16, 16, 49, 22, 597, DateTimeKind.Local).AddTicks(1789) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Amount",
                table: "tblAttendanceFee",
                type: "int",
                nullable: false,
                comment: "單堂學費",
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)",
                oldComment: "單堂學費");

            migrationBuilder.AlterColumn<int>(
                name: "AdjustmentAmount",
                table: "tblAttendanceFee",
                type: "int",
                nullable: false,
                comment: "單堂增減金額",
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)",
                oldComment: "單堂增減金額");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4898), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4899) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4901), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4901) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4903), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4903) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4904), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4904) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4882), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4894) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4896), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4896) });
        }
    }
}
