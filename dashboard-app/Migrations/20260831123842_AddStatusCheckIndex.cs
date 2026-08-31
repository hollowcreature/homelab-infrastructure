using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DashboardApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusCheckIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StatusChecks_ServiceId",
                table: "StatusChecks");

            migrationBuilder.CreateIndex(
                name: "IX_StatusChecks_ServiceId_CheckedAt",
                table: "StatusChecks",
                columns: new[] { "ServiceId", "CheckedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StatusChecks_ServiceId_CheckedAt",
                table: "StatusChecks");

            migrationBuilder.CreateIndex(
                name: "IX_StatusChecks_ServiceId",
                table: "StatusChecks",
                column: "ServiceId");
        }
    }
}
