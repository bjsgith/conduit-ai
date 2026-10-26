using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConduitAI.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadFollowUps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeadFollowUps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadFollowUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadFollowUps_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadFollowUps_CompletedAtUtc_DueAtUtc",
                table: "LeadFollowUps",
                columns: new[] { "CompletedAtUtc", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadFollowUps_LeadId_CompletedAtUtc_DueAtUtc",
                table: "LeadFollowUps",
                columns: new[] { "LeadId", "CompletedAtUtc", "DueAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadFollowUps");
        }
    }
}
