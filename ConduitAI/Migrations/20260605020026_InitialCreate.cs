using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConduitAI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LeadSource = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    LeadScore = table.Column<int>(type: "INTEGER", nullable: false),
                    UrgencyLevel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BuyingIntent = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RecommendedNextAction = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModelName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadAnalyses_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeadInteractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InteractionType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadInteractions_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: true),
                    RawNotes = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: false),
                    StructuredSummary = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    KeyFactsJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    RisksJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    RecommendedNextAction = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModelName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingNotes_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadAnalyses_LeadId_GeneratedAt",
                table: "LeadAnalyses",
                columns: new[] { "LeadId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadInteractions_LeadId_OccurredAt",
                table: "LeadInteractions",
                columns: new[] { "LeadId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Status",
                table: "Leads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_UpdatedAt",
                table: "Leads",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotes_CreatedAt",
                table: "MeetingNotes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotes_LeadId",
                table: "MeetingNotes",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadAnalyses");

            migrationBuilder.DropTable(
                name: "LeadInteractions");

            migrationBuilder.DropTable(
                name: "MeetingNotes");

            migrationBuilder.DropTable(
                name: "Leads");
        }
    }
}
