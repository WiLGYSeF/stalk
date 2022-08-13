using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wilgysef.Stalk.EntityFrameworkCore.Migrations
{
    public partial class BackgroundJobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackgroundJobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    NextRun = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaximumLifetime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Abandoned = table.Column<bool>(type: "INTEGER", nullable: false),
                    JobArgsName = table.Column<string>(type: "TEXT", nullable: false),
                    JobArgs = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobs");
        }
    }
}
