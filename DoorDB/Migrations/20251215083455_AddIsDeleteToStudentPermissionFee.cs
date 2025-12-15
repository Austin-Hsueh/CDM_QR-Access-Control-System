using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddIsDeleteToStudentPermissionFee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDelete",
                table: "tblStudentPermissionFee",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                comment: "是否刪除");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1623), new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1624) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1629), new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1630) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1631), new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1632) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1633), new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1634) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1594), new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1616) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1618), new DateTime(2025, 12, 15, 16, 34, 55, 413, DateTimeKind.Local).AddTicks(1619) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDelete",
                table: "tblStudentPermissionFee");

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
    }
}
