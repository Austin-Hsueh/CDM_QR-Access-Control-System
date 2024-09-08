using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class addTypeToTblUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "tblUser",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "選課狀態 預設0,1在學,2停課,3約課");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7432), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7433) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7435), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7435) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7437), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7437) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7438), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7438) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7418), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7427) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7430), new DateTime(2024, 9, 5, 23, 5, 26, 442, DateTimeKind.Local).AddTicks(7430) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "tblUser");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4639), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4640) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4646), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4647) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4650), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4652) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4655), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4656) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4609), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4623) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4630), new DateTime(2024, 7, 31, 0, 7, 15, 441, DateTimeKind.Local).AddTicks(4631) });
        }
    }
}
