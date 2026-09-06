using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    /// <summary>
    /// 缺課日期拆分為學生 / 老師兩欄
    /// 既有的 AbsenceDates 內容視為學生缺課，以 RenameColumn 保留資料
    /// </summary>
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(DoorDB.DoorDbContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260728120000_SplitAbsenceDatesByRole")]
    public partial class SplitAbsenceDatesByRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AbsenceDates",
                table: "tblStudentPermissionFee",
                newName: "StudentAbsenceDates");

            migrationBuilder.AlterColumn<string>(
                name: "StudentAbsenceDates",
                table: "tblStudentPermissionFee",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                comment: "學生缺課日期(逗號分隔 yyyy-MM-dd)",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "缺課日期(逗號分隔 yyyy-MM-dd)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TeacherAbsenceDates",
                table: "tblStudentPermissionFee",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                comment: "老師缺課日期(逗號分隔 yyyy-MM-dd)")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeacherAbsenceDates",
                table: "tblStudentPermissionFee");

            migrationBuilder.AlterColumn<string>(
                name: "StudentAbsenceDates",
                table: "tblStudentPermissionFee",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                comment: "缺課日期(逗號分隔 yyyy-MM-dd)",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "學生缺課日期(逗號分隔 yyyy-MM-dd)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.RenameColumn(
                name: "StudentAbsenceDates",
                table: "tblStudentPermissionFee",
                newName: "AbsenceDates");
        }
    }
}
