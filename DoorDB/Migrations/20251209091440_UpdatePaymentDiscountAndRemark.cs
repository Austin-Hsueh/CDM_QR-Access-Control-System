using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class UpdatePaymentDiscountAndRemark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountAmount",
                table: "tblPayment",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "總額折扣");

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "tblPayment",
                type: "longtext",
                nullable: true,
                comment: "備註")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1865), new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1865) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1867), new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1867) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1868), new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1869) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1870), new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1870) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1846), new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1859) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1861), new DateTime(2025, 12, 9, 17, 14, 40, 371, DateTimeKind.Local).AddTicks(1861) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "tblPayment");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "tblPayment");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2167), new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2167) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2169), new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2170) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2171), new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2171) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2172), new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2173) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2149), new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2161) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2163), new DateTime(2025, 12, 9, 11, 57, 5, 473, DateTimeKind.Local).AddTicks(2163) });

        }
    }
}
