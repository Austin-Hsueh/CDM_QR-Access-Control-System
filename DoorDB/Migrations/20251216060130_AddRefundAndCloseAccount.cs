using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddRefundAndCloseAccount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblCloseAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CloseDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    YesterdayPettyIncome = table.Column<int>(type: "int", nullable: false, comment: "昨日零用金結餘"),
                    BusinessIncome = table.Column<int>(type: "int", nullable: false, comment: "營業收入 (學生學費)"),
                    CloseAccountAmount = table.Column<int>(type: "int", nullable: false, comment: "關帳結算金額 (昨日零用金收支 + 營業收入)"),
                    DepositAmount = table.Column<int>(type: "int", nullable: false, comment: "提存金額"),
                    PettyIncome = table.Column<int>(type: "int", nullable: false, comment: "零用金結餘"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCloseAccount", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tblRefund",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "流水號")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentPermissionFeeId = table.Column<int>(type: "int", nullable: false, comment: "學生權限費用Id"),
                    RefundDate = table.Column<string>(type: "longtext", nullable: false, comment: "退款日期")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefundAmount = table.Column<int>(type: "int", nullable: false, comment: "退款金額"),
                    Remark = table.Column<string>(type: "longtext", nullable: true, comment: "備註")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "建立時間"),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "修改時間"),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "是否刪除")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblRefund", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblRefund_tblStudentPermissionFee_StudentPermissionFeeId",
                        column: x => x.StudentPermissionFeeId,
                        principalTable: "tblStudentPermissionFee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4898), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4899) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4901), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4901) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4903), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4903) });

            migrationBuilder.UpdateData(
                table: "tblRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4904), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4904) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4882), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4894) });

            migrationBuilder.UpdateData(
                table: "tblUser",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CreateTime", "ModifiedTime" },
                values: new object[] { new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4896), new DateTime(2025, 12, 16, 14, 1, 30, 306, DateTimeKind.Local).AddTicks(4896) });

            migrationBuilder.CreateIndex(
                name: "IX_tblRefund_StudentPermissionFeeId",
                table: "tblRefund",
                column: "StudentPermissionFeeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblCloseAccount");

            migrationBuilder.DropTable(
                name: "tblRefund");

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
    }
}
