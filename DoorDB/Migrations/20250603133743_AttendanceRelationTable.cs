using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AttendanceRelationTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblAttendance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentPermissionId = table.Column<int>(type: "int", nullable: false),
                    AttendanceDate = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttendanceType = table.Column<int>(type: "int", nullable: false),
                    IsTrigger = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblAttendance_tblStudentPermission_StudentPermissionId",
                        column: x => x.StudentPermissionId,
                        principalTable: "tblStudentPermission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblAttendance_tblUser_ModifiedUserId",
                        column: x => x.ModifiedUserId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblPayment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentPermissionId = table.Column<int>(type: "int", nullable: false),
                    PayDate = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pay = table.Column<int>(type: "int", nullable: false),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblPayment_tblStudentPermission_StudentPermissionId",
                        column: x => x.StudentPermissionId,
                        principalTable: "tblStudentPermission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblPayment_tblUser_ModifiedUserId",
                        column: x => x.ModifiedUserId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttendance_ModifiedUserId",
                table: "tblAttendance",
                column: "ModifiedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttendance_StudentPermissionId",
                table: "tblAttendance",
                column: "StudentPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPayment_ModifiedUserId",
                table: "tblPayment",
                column: "ModifiedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPayment_StudentPermissionId",
                table: "tblPayment",
                column: "StudentPermissionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblAttendance");

            migrationBuilder.DropTable(
                name: "tblPayment");
        }
    }
}
