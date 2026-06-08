using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConduitAI.Migrations
{
    /// <inheritdoc />
    public partial class CascadeMeetingNotesOnLeadDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeetingNotes_Leads_LeadId",
                table: "MeetingNotes");

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingNotes_Leads_LeadId",
                table: "MeetingNotes",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeetingNotes_Leads_LeadId",
                table: "MeetingNotes");

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingNotes_Leads_LeadId",
                table: "MeetingNotes",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
