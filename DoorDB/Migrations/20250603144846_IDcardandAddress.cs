using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class IDcardandAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "tblUser",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IDcard",
                table: "tblUser",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "tblUser");

            migrationBuilder.DropColumn(
                name: "IDcard",
                table: "tblUser");
        }
    }
}
