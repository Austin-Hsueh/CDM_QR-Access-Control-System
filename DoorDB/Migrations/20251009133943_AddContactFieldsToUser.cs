using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddContactFieldsToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "tblUser",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "tblUser",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RelationshipTitle",
                table: "tblUser",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2440), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2442) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2444), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2444) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2445), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2446) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2448), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2449) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "ContactPerson", "ContactPhone", "CreateTime", "ModifiedTime", "RelationshipTitle" },
                values: new object[] { "", "", new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2422), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2434), "" });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "ContactPerson", "ContactPhone", "CreateTime", "ModifiedTime", "RelationshipTitle" },
                values: new object[] { "", "", new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2437), new DateTime(2025, 10, 9, 21, 39, 43, 546, DateTimeKind.Local).AddTicks(2438), "" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "tblUser");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "tblUser");

            migrationBuilder.DropColumn(
                name: "RelationshipTitle",
                table: "tblUser");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(632), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(633) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(635), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(636) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(637), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(637) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(638), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(638) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(618), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(626) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(629), new DateTime(2025, 10, 1, 23, 8, 52, 240, DateTimeKind.Local).AddTicks(629) });
        }
    }
}
