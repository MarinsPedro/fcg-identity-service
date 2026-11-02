using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiapCloudGames.Identity.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeServiceClientPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_client_audiences",
                schema: "identity",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    audience = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_client_audiences", x => new { x.client_id, x.audience });
                    table.ForeignKey(
                        name: "FK_service_client_audiences_service_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "identity",
                        principalTable: "service_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_client_scopes",
                schema: "identity",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_client_scopes", x => new { x.client_id, x.scope });
                    table.ForeignKey(
                        name: "FK_service_client_scopes_service_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "identity",
                        principalTable: "service_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_client_audiences_audience",
                schema: "identity",
                table: "service_client_audiences",
                column: "audience");

            migrationBuilder.CreateIndex(
                name: "IX_service_client_scopes_scope",
                schema: "identity",
                table: "service_client_scopes",
                column: "scope");

            migrationBuilder.Sql(
                """
                INSERT INTO identity.service_client_audiences (client_id, audience)
                SELECT clients.client_id, permission.value
                FROM identity.service_clients AS clients
                CROSS JOIN LATERAL regexp_split_to_table(
                    btrim(clients.allowed_audiences),
                    '[[:space:]]+') AS permission(value)
                WHERE permission.value <> ''
                ON CONFLICT DO NOTHING;

                INSERT INTO identity.service_client_scopes (client_id, scope)
                SELECT clients.client_id, permission.value
                FROM identity.service_clients AS clients
                CROSS JOIN LATERAL regexp_split_to_table(
                    btrim(clients.allowed_scopes),
                    '[[:space:]]+') AS permission(value)
                WHERE permission.value <> ''
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.DropColumn(
                name: "allowed_audiences",
                schema: "identity",
                table: "service_clients");

            migrationBuilder.DropColumn(
                name: "allowed_scopes",
                schema: "identity",
                table: "service_clients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "allowed_audiences",
                schema: "identity",
                table: "service_clients",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "allowed_scopes",
                schema: "identity",
                table: "service_clients",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE identity.service_clients AS clients
                SET allowed_audiences = COALESCE(
                    (
                        SELECT string_agg(
                            permissions.audience,
                            ' '
                            ORDER BY permissions.audience)
                        FROM identity.service_client_audiences AS permissions
                        WHERE permissions.client_id = clients.client_id
                    ),
                    ''),
                    allowed_scopes = COALESCE(
                    (
                        SELECT string_agg(
                            permissions.scope,
                            ' '
                            ORDER BY permissions.scope)
                        FROM identity.service_client_scopes AS permissions
                        WHERE permissions.client_id = clients.client_id
                    ),
                    '');
                """);

            migrationBuilder.DropTable(
                name: "service_client_audiences",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "service_client_scopes",
                schema: "identity");
        }
    }
}
