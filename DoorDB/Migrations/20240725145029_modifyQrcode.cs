using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class modifyQrcode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoorTime",
                table: "tblQRCodeStorage");

            migrationBuilder.DropColumn(
                name: "IsDelete",
                table: "tblQRCodeStorage");

            migrationBuilder.DropColumn(
                name: "IsEnable",
                table: "tblQRCodeStorage");

            migrationBuilder.DropColumn(
                name: "QRcodeType",
                table: "tblQRCodeStorage");

            migrationBuilder.AddColumn<string>(
                name: "qrcodeTxt",
                table: "tblQRCodeStorage",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "userTag",
                table: "tblQRCodeStorage",
                type: "int",
                nullable: false,
                defaultValue: 0);

            
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "qrcodeTxt",
                table: "tblQRCodeStorage");

            migrationBuilder.DropColumn(
                name: "userTag",
                table: "tblQRCodeStorage");

            migrationBuilder.AddColumn<DateTime>(
                name: "DoorTime",
                table: "tblQRCodeStorage",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDelete",
                table: "tblQRCodeStorage",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnable",
                table: "tblQRCodeStorage",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "QRcodeType",
                table: "tblQRCodeStorage",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
