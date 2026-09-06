using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    /// <summary>
    /// 為 tblStudentPermissionFee 增加「課程期限」與「缺課日期」欄位
    /// 缺課日期以逗號分隔的 yyyy-MM-dd 字串儲存
    /// </summary>
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(DoorDB.DoorDbContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260728000000_AddDeadlineAndAbsenceToStudentPermissionFee")]
    public partial class AddDeadlineAndAbsenceToStudentPermissionFee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CourseDeadline",
                table: "tblStudentPermissionFee",
                type: "datetime(6)",
                nullable: true,
                comment: "課程期限");

            migrationBuilder.AddColumn<string>(
                name: "AbsenceDates",
                table: "tblStudentPermissionFee",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                comment: "缺課日期(逗號分隔 yyyy-MM-dd)")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseDeadline",
                table: "tblStudentPermissionFee");

            migrationBuilder.DropColumn(
                name: "AbsenceDates",
                table: "tblStudentPermissionFee");
        }
    }
}
