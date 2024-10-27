using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class modifytblBookingLogSerial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Serial",
                table: "tblBookingLog",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_tblBookingLog",
                table: "tblBookingLog",
                column: "Serial");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Serial",
                table: "tblBookingLog");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tblBookingLog",
                table: "tblBookingLog",
                column: "Id");
        }
    }
}
