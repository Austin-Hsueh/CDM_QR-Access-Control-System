using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddParentIdToTblUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "tblUser",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1899), new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1899) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1900), new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1901) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1902), new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1902) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1903), new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1903) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1882), new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1894) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1896), new DateTime(2025, 7, 7, 21, 50, 32, 794, DateTimeKind.Local).AddTicks(1897) });

            migrationBuilder.CreateIndex(
                name: "IX_tblUser_ParentId",
                table: "tblUser",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_tblUser_tblUser_ParentId",
                table: "tblUser",
                column: "ParentId",
                principalTable: "tblUser",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblUser_tblUser_ParentId",
                table: "tblUser");

            migrationBuilder.DropIndex(
                name: "IX_tblUser_ParentId",
                table: "tblUser");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "tblUser");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9879), new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9879) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9881), new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9881) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9882), new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9882) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9883), new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9884) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9864), new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9873) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9876), new DateTime(2025, 6, 3, 23, 44, 6, 302, DateTimeKind.Local).AddTicks(9877) });
        }
    }
}
