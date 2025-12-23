using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddRemarkToCourseFee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "tblCourseFee",
                type: "longtext",
                nullable: true,
                comment: "課程說明")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4584), new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4585) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4587), new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4587) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4588), new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4589) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4590), new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4590) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4566), new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4578) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4580), new DateTime(2025, 12, 23, 15, 12, 42, 346, DateTimeKind.Local).AddTicks(4580) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Remark",
                table: "tblCourseFee");

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
    }
}
