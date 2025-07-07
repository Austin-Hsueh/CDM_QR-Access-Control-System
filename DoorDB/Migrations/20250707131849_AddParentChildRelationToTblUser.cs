using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddParentChildRelationToTblUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "tblUser",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblUser_ParentId",
                table: "tblUser",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_tblUser_tblUser_ParentId",
                table: "tblUser",
                column: "ParentId",
                principalTable: "tblUser",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblUser_tblUser_ParentId",
                table: "tblUser");

            migrationBuilder.DropIndex(
                name: "IX_tblUser_ParentId",
                table: "tblUser");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "tblUser");
        }
    }
}
