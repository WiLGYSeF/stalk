using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wilgysef.Stalk.EntityFrameworkCore.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Started = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Finished = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DelayedUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConfigJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Uri = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    ItemData = table.Column<string>(type: "TEXT", nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Started = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Finished = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DelayedUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Result_Success = table.Column<bool>(type: "INTEGER", nullable: true),
                    Result_ErrorCode = table.Column<string>(type: "TEXT", nullable: true),
                    Result_ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    Result_ErrorDetail = table.Column<string>(type: "TEXT", nullable: true),
                    ParentTaskId = table.Column<long>(type: "INTEGER", nullable: true),
                    JobId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTasks_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobTasks_JobTasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "JobTasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobTasks_JobId",
                table: "JobTasks",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTasks_ParentTaskId",
                table: "JobTasks",
                column: "ParentTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobTasks");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
