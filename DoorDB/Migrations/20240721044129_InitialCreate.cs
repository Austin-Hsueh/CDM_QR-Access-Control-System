using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblAuditLog",
                columns: table => new
                {
                    Serial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IP = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblAuditLog", x => x.Serial);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblPermission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "權限項目Id")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PermissionGroupId = table.Column<int>(type: "int", nullable: false, comment: "權限項目所屬群組([tblPermissionGroup].[Id])"),
                    Name = table.Column<string>(type: "longtext", nullable: false, comment: "權限項目名稱")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameI18n = table.Column<string>(type: "longtext", nullable: false, comment: "權限項目名稱(i18n)")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "權限時間起"),
                    DateTo = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "權限時訖"),
                    PermissionLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPermission", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblPermissionGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameI18n = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPermissionGroup", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblQRCodeStorage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QRCodeData = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QRcodeType = table.Column<string>(type: "varchar(10)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DoorTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblQRCodeStorage", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatorUserId = table.Column<int>(type: "int", nullable: false),
                    IsEnable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanDelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblRole", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Secret = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    locale = table.Column<int>(type: "int", nullable: false),
                    AccountType = table.Column<string>(type: "varchar(10)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastLoginIP = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastLoginTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblUser", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TblPermissionTblUser",
                columns: table => new
                {
                    PermissionsId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblPermissionTblUser", x => new { x.PermissionsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_TblPermissionTblUser_tblPermission_PermissionsId",
                        column: x => x.PermissionsId,
                        principalTable: "tblPermission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblPermissionTblUser_tblUser_UsersId",
                        column: x => x.UsersId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TblQRCodeStorageTblUser",
                columns: table => new
                {
                    QRCodesId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblQRCodeStorageTblUser", x => new { x.QRCodesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_TblQRCodeStorageTblUser_tblQRCodeStorage_QRCodesId",
                        column: x => x.QRCodesId,
                        principalTable: "tblQRCodeStorage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblQRCodeStorageTblUser_tblUser_UsersId",
                        column: x => x.UsersId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TblRoleTblUser",
                columns: table => new
                {
                    RolesId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblRoleTblUser", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_TblRoleTblUser_tblRole_RolesId",
                        column: x => x.RolesId,
                        principalTable: "tblRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblRoleTblUser_tblUser_UsersId",
                        column: x => x.UsersId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "tblPermissionGroup",
                columns: new[] { "Id", "Name", "NameI18n" },
                values: new object[,]
                {
                    { 51, "大門", "" },
                    { 52, "Car教室", "" },
                    { 53, "Sunny教室", "" },
                    { 54, "儲藏室", "" }
                });

            migrationBuilder.InsertData(
                table: "tblRole",
                columns: new[] { "Id", "CanDelete", "CreatedTime", "CreatorUserId", "Description", "IsDelete", "IsEnable", "ModifiedTime", "Name" },
                values: new object[,]
                {
                    { 1, false, new DateTime(2024, 7, 21, 12, 41, 29, 88, DateTimeKind.Local).AddTicks(7569), 1, "管理者", false, true, new DateTime(2024, 7, 21, 12, 41, 29, 88, DateTimeKind.Local).AddTicks(7570), "Admin" },
                    { 2, false, new DateTime(2024, 7, 21, 12, 41, 29, 88, DateTimeKind.Local).AddTicks(7573), 1, "老師", false, true, new DateTime(2024, 7, 21, 12, 41, 29, 88, DateTimeKind.Local).AddTicks(7574), "User" },
                    { 3, false, new DateTime(2024, 7, 21, 12, 41, 29, 88, DateTimeKind.Local).AddTicks(7576), 1, "學生", false, true, new DateTime(2024, 7, 21, 12, 41, 29, 88, DateTimeKind.Local).AddTicks(7577), "User" },
                    { 4, false, new DateTime(2024, 7, 21, 12, 41, 29, 88, DateTimeKind.Local).AddTicks(7579), 1, "值班人員", false, true, new DateTime(2024, 7, 21, 12, 41, 29, 88, DateTimeKind.Local).AddTicks(7580), "User" }
                });

            migrationBuilder.InsertData(
                table: "tblUser",
                columns: new[] { "Id", "AccountType", "CreateTime", "DisplayName", "Email", "IsDelete", "IsEnable", "LastLoginIP", "LastLoginTime", "ModifiedTime", "Secret", "Username", "locale" },
                values: new object[] { 51, "LOCAL", new DateTime(2024, 7, 21, 12, 41, 29, 88, DateTimeKind.Local).AddTicks(7552), "Administrator", "", false, true, "", null, new DateTime(2024, 7, 21, 12, 41, 29, 88, DateTimeKind.Local).AddTicks(7563), "1qaz2wsx", "admin", 1 });

            migrationBuilder.CreateIndex(
                name: "IX_TblPermissionTblUser_UsersId",
                table: "TblPermissionTblUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_TblQRCodeStorageTblUser_UsersId",
                table: "TblQRCodeStorageTblUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_TblRoleTblUser_UsersId",
                table: "TblRoleTblUser",
                column: "UsersId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblAuditLog");

            migrationBuilder.DropTable(
                name: "tblPermissionGroup");

            migrationBuilder.DropTable(
                name: "TblPermissionTblUser");

            migrationBuilder.DropTable(
                name: "TblQRCodeStorageTblUser");

            migrationBuilder.DropTable(
                name: "TblRoleTblUser");

            migrationBuilder.DropTable(
                name: "tblPermission");

            migrationBuilder.DropTable(
                name: "tblQRCodeStorage");

            migrationBuilder.DropTable(
                name: "tblRole");

            migrationBuilder.DropTable(
                name: "tblUser");
        }
    }
}
