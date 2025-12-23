using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddHoursToStudentPermissionFee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Hours",
                table: "tblStudentPermissionFee",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m,
                comment: "課程時數");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4351), new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4352) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4355), new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4355) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4359), new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4360) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4361), new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4362) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4332), new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4343) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4346), new DateTime(2025, 12, 23, 9, 39, 34, 803, DateTimeKind.Local).AddTicks(4347) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hours",
                table: "tblStudentPermissionFee");

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
    }
}
