using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wilgysef.Stalk.EntityFrameworkCore.Migrations
{
    public partial class DownloadRequestData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "DownloadRequestData_Data",
                table: "JobTasks",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DownloadRequestData_Headers",
                table: "JobTasks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DownloadRequestData_Method",
                table: "JobTasks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadRequestData_Data",
                table: "JobTasks");

            migrationBuilder.DropColumn(
                name: "DownloadRequestData_Headers",
                table: "JobTasks");

            migrationBuilder.DropColumn(
                name: "DownloadRequestData_Method",
                table: "JobTasks");
        }
    }
}
