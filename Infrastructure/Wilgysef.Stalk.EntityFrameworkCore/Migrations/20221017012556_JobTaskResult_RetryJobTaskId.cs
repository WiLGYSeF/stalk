using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wilgysef.Stalk.EntityFrameworkCore.Migrations
{
    public partial class JobTaskResult_RetryJobTaskId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Result_RetryJobTaskId",
                table: "JobTasks",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Result_RetryJobTaskId",
                table: "JobTasks");
        }
    }
}
