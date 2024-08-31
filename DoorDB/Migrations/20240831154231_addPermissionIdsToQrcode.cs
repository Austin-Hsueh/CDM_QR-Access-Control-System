using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class addPermissionIdsToQrcode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TblPermissionTblQRCodeStorage",
                columns: table => new
                {
                    PermissionsId = table.Column<int>(type: "int", nullable: false),
                    QRCodeStoragesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblPermissionTblQRCodeStorage", x => new { x.PermissionsId, x.QRCodeStoragesId });
                    table.ForeignKey(
                        name: "FK_TblPermissionTblQRCodeStorage_tblPermission_PermissionsId",
                        column: x => x.PermissionsId,
                        principalTable: "tblPermission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblPermissionTblQRCodeStorage_tblQRCodeStorage_QRCodeStorage~",
                        column: x => x.QRCodeStoragesId,
                        principalTable: "tblQRCodeStorage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TblQRCodeStorageTblStudentPermission",
                columns: table => new
                {
                    StudentPermissionsId = table.Column<int>(type: "int", nullable: false),
                    StudentQRCodeStoragesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblQRCodeStorageTblStudentPermission", x => new { x.StudentPermissionsId, x.StudentQRCodeStoragesId });
                    table.ForeignKey(
                        name: "FK_TblQRCodeStorageTblStudentPermission_tblQRCodeStorage_Studen~",
                        column: x => x.StudentQRCodeStoragesId,
                        principalTable: "tblQRCodeStorage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblQRCodeStorageTblStudentPermission_tblStudentPermission_St~",
                        column: x => x.StudentPermissionsId,
                        principalTable: "tblStudentPermission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(454), new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(454) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(456), new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(456) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(457), new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(458) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(459), new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(459) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(439), new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(448) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(452), new DateTime(2024, 8, 31, 23, 42, 30, 811, DateTimeKind.Local).AddTicks(452) });

            migrationBuilder.CreateIndex(
                name: "IX_TblPermissionTblQRCodeStorage_QRCodeStoragesId",
                table: "TblPermissionTblQRCodeStorage",
                column: "QRCodeStoragesId");

            migrationBuilder.CreateIndex(
                name: "IX_TblQRCodeStorageTblStudentPermission_StudentQRCodeStoragesId",
                table: "TblQRCodeStorageTblStudentPermission",
                column: "StudentQRCodeStoragesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TblPermissionTblQRCodeStorage");

            migrationBuilder.DropTable(
                name: "TblQRCodeStorageTblStudentPermission");

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
