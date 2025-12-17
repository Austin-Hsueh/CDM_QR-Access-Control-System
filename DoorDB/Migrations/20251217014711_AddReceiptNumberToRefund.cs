using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddReceiptNumberToRefund : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "tblRefund",
                type: "longtext",
                nullable: true,
                comment: "結帳單號")
                .Annotation("MySql:CharSet", "utf8mb4");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "tblRefund");

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
    }
}
