using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiapCloudGames.Identity.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEventCursorIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_security_audit_events_occurred_at_utc",
                schema: "identity",
                table: "security_audit_events");

            migrationBuilder.DropIndex(
                name: "IX_security_audit_events_subject_type_subject_id",
                schema: "identity",
                table: "security_audit_events");

            migrationBuilder.CreateIndex(
                name: "IX_security_audit_events_actor_client_id_occurred_at_utc_id",
                schema: "identity",
                table: "security_audit_events",
                columns: new[] { "actor_client_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_security_audit_events_event_type_occurred_at_utc_id",
                schema: "identity",
                table: "security_audit_events",
                columns: new[] { "event_type", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_security_audit_events_occurred_at_utc_id",
                schema: "identity",
                table: "security_audit_events",
                columns: new[] { "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_security_audit_events_subject_type_subject_id_occurred_at_u~",
                schema: "identity",
                table: "security_audit_events",
                columns: new[] { "subject_type", "subject_id", "occurred_at_utc", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_security_audit_events_actor_client_id_occurred_at_utc_id",
                schema: "identity",
                table: "security_audit_events");

            migrationBuilder.DropIndex(
                name: "IX_security_audit_events_event_type_occurred_at_utc_id",
                schema: "identity",
                table: "security_audit_events");

            migrationBuilder.DropIndex(
                name: "IX_security_audit_events_occurred_at_utc_id",
                schema: "identity",
                table: "security_audit_events");

            migrationBuilder.DropIndex(
                name: "IX_security_audit_events_subject_type_subject_id_occurred_at_u~",
                schema: "identity",
                table: "security_audit_events");

            migrationBuilder.CreateIndex(
                name: "IX_security_audit_events_occurred_at_utc",
                schema: "identity",
                table: "security_audit_events",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_security_audit_events_subject_type_subject_id",
                schema: "identity",
                table: "security_audit_events",
                columns: new[] { "subject_type", "subject_id" });
        }
    }
}
