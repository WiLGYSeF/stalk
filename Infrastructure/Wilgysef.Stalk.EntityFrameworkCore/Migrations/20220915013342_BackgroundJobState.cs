using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wilgysef.Stalk.EntityFrameworkCore.Migrations
{
    public partial class BackgroundJobState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Abandoned",
                table: "BackgroundJobs");

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "BackgroundJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "BackgroundJobs");

            migrationBuilder.AddColumn<bool>(
                name: "Abandoned",
                table: "BackgroundJobs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
