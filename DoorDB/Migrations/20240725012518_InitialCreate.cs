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
                name: "tblPermissionGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
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
                    Phone = table.Column<string>(type: "longtext", nullable: false)
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
                name: "tblPermission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "權限項目Id")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false, comment: "權限項目所屬使用者([tblUser].[Id])"),
                    IsEnable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateFrom = table.Column<string>(type: "varchar(10)", nullable: false, comment: "權限日期起")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateTo = table.Column<string>(type: "varchar(10)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeFrom = table.Column<string>(type: "varchar(5)", nullable: false, comment: "權限時間起")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeTo = table.Column<string>(type: "varchar(5)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Days = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PermissionLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblPermission_tblUser_UserId",
                        column: x => x.UserId,
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

            migrationBuilder.CreateTable(
                name: "TblPermissionTblPermissionGroup",
                columns: table => new
                {
                    PermissionGroupsId = table.Column<int>(type: "int", nullable: false),
                    PermissionsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblPermissionTblPermissionGroup", x => new { x.PermissionGroupsId, x.PermissionsId });
                    table.ForeignKey(
                        name: "FK_TblPermissionTblPermissionGroup_tblPermission_PermissionsId",
                        column: x => x.PermissionsId,
                        principalTable: "tblPermission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblPermissionTblPermissionGroup_tblPermissionGroup_Permissio~",
                        column: x => x.PermissionGroupsId,
                        principalTable: "tblPermissionGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "tblPermissionGroup",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "大門" },
                    { 2, "Car教室" },
                    { 3, "Sunny教室" },
                    { 4, "儲藏室" }
                });

            migrationBuilder.InsertData(
                table: "tblRole",
                columns: new[] { "Id", "CanDelete", "CreatedTime", "CreatorUserId", "Description", "IsDelete", "IsEnable", "ModifiedTime", "Name" },
                values: new object[,]
                {
                    { 1, false, new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1606), 1, "管理者", false, true, new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1607), "Admin" },
                    { 2, false, new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1613), 1, "老師", false, true, new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1615), "User" },
                    { 3, false, new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1619), 1, "學生", false, true, new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1620), "User" },
                    { 4, false, new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1623), 1, "值班人員", false, true, new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1624), "User" }
                });

            migrationBuilder.InsertData(
                table: "tblUser",
                columns: new[] { "Id", "AccountType", "CreateTime", "DisplayName", "Email", "IsDelete", "IsEnable", "LastLoginIP", "LastLoginTime", "ModifiedTime", "Phone", "Secret", "Username", "locale" },
                values: new object[,]
                {
                    { 51, "LOCAL", new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1578), "Administrator", "", false, true, "", null, new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1591), "0", "1qaz2wsx", "admin", 1 },
                    { 52, "LOCAL", new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1598), "臨時大門", "", false, true, "", null, new DateTime(2024, 7, 25, 9, 25, 17, 461, DateTimeKind.Local).AddTicks(1599), "0", "1qaz2wsx", "TemDoor", 1 }
                });

            migrationBuilder.InsertData(
                table: "TblRoleTblUser",
                columns: new[] { "RolesId", "UsersId" },
                values: new object[,]
                {
                    { 1, 51 },
                    { 4, 52 }
                });

            migrationBuilder.InsertData(
                table: "tblPermission",
                columns: new[] { "Id", "DateFrom", "DateTo", "Days", "IsDelete", "IsEnable", "PermissionLevel", "TimeFrom", "TimeTo", "UserId" },
                values: new object[,]
                {
                    { 1, "2024/07/21", "2124/07/21", "1,2,3,4,5,6,7", false, true, 1, "00:00", "24:00", 51 },
                    { 2, "2024/07/21", "2124/07/21", "", false, true, 1, "00:00", "24:00", 52 }
                });

            migrationBuilder.InsertData(
                table: "TblPermissionTblPermissionGroup",
                columns: new[] { "PermissionGroupsId", "PermissionsId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblPermission_UserId",
                table: "tblPermission",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblPermissionTblPermissionGroup_PermissionsId",
                table: "TblPermissionTblPermissionGroup",
                column: "PermissionsId");

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
                name: "TblPermissionTblPermissionGroup");

            migrationBuilder.DropTable(
                name: "TblQRCodeStorageTblUser");

            migrationBuilder.DropTable(
                name: "TblRoleTblUser");

            migrationBuilder.DropTable(
                name: "tblPermission");

            migrationBuilder.DropTable(
                name: "tblPermissionGroup");

            migrationBuilder.DropTable(
                name: "tblQRCodeStorage");

            migrationBuilder.DropTable(
                name: "tblRole");

            migrationBuilder.DropTable(
                name: "tblUser");
        }
    }
}
