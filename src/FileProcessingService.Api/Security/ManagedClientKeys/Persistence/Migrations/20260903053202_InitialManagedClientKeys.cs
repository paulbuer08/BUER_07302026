using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileProcessingService.Api.Security.ManagedClientKeys.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialManagedClientKeys : Migration
    {
        /// <inheritdoc />
        ///
        private static readonly string[] ApiKeyCredentialClientRevokedIndexColumns =
        [
            "client_application_id",
            "revoked_at_utc"
        ];

        private static readonly string[] ApiKeyLifecycleEventCredentialTimeIndexColumns =
        [
            "api_key_credential_id",
            "occurred_at_utc"
        ];

        private static readonly string[] ClientApplicationOrganizationStatusIndexColumns =
        [
            "organization_id",
            "status"
        ];


        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "security");

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    normalized_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                    table.CheckConstraint("ck_organizations_code_not_blank", "char_length(btrim(code)) > 0");
                    table.CheckConstraint("ck_organizations_display_name_not_blank", "char_length(btrim(display_name)) > 0");
                    table.CheckConstraint("ck_organizations_normalized_code_not_blank", "char_length(btrim(normalized_code)) > 0");
                    table.CheckConstraint("ck_organizations_status", "status IN ('PendingApproval', 'Active', 'Suspended', 'Closed')");
                });

            migrationBuilder.CreateTable(
                name: "client_applications",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    disabled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_applications", x => x.id);
                    table.CheckConstraint("ck_client_applications_display_name_not_blank", "char_length(btrim(display_name)) > 0");
                    table.CheckConstraint("ck_client_applications_identifier_not_blank", "char_length(btrim(client_identifier)) > 0");
                    table.CheckConstraint("ck_client_applications_status", "status IN ('PendingActivation', 'Active', 'Suspended', 'Disabled')");
                    table.ForeignKey(
                        name: "FK_client_applications_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "security",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "api_key_credentials",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    secret_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    hash_algorithm_version = table.Column<short>(type: "smallint", nullable: false),
                    issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    not_before_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    replaces_credential_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_key_credentials", x => x.id);
                    table.CheckConstraint("ck_api_key_credentials_expiration", "expires_at_utc IS NULL OR expires_at_utc > not_before_utc");
                    table.CheckConstraint("ck_api_key_credentials_hash_version", "hash_algorithm_version >= 1");
                    table.CheckConstraint("ck_api_key_credentials_key_id_not_blank", "char_length(btrim(key_id)) > 0");
                    table.CheckConstraint("ck_api_key_credentials_not_before", "not_before_utc >= issued_at_utc");
                    table.CheckConstraint("ck_api_key_credentials_replacement", "replaces_credential_id IS NULL OR replaces_credential_id <> id");
                    table.CheckConstraint("ck_api_key_credentials_revocation", "(revoked_at_utc IS NULL AND revocation_reason IS NULL) OR (revoked_at_utc IS NOT NULL AND revocation_reason IS NOT NULL AND char_length(btrim(revocation_reason)) > 0)");
                    table.CheckConstraint("ck_api_key_credentials_version_one_hash_length", "hash_algorithm_version <> 1 OR octet_length(secret_hash) = 32");
                    table.ForeignKey(
                        name: "FK_api_key_credentials_api_key_credentials_replaces_credential~",
                        column: x => x.replaces_credential_id,
                        principalSchema: "security",
                        principalTable: "api_key_credentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_api_key_credentials_client_applications_client_application_~",
                        column: x => x.client_application_id,
                        principalSchema: "security",
                        principalTable: "client_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "api_key_credential_scopes",
                schema: "security",
                columns: table => new
                {
                    api_key_credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_key_credential_scopes", x => new { x.api_key_credential_id, x.scope });
                    table.CheckConstraint("ck_api_key_credential_scopes_scope_not_blank", "char_length(btrim(scope)) > 0");
                    table.ForeignKey(
                        name: "FK_api_key_credential_scopes_api_key_credentials_api_key_crede~",
                        column: x => x.api_key_credential_id,
                        principalSchema: "security",
                        principalTable: "api_key_credentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "api_key_lifecycle_events",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    api_key_credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_key_lifecycle_events", x => x.id);
                    table.CheckConstraint("ck_api_key_lifecycle_events_actor_not_blank", "char_length(btrim(actor_identifier)) > 0");
                    table.CheckConstraint("ck_api_key_lifecycle_events_event_type", "event_type IN ('Issued', 'Activated', 'RotationStarted', 'Revoked')");
                    table.CheckConstraint("ck_api_key_lifecycle_events_reason_not_blank", "reason IS NULL OR char_length(btrim(reason)) > 0");
                    table.ForeignKey(
                        name: "FK_api_key_lifecycle_events_api_key_credentials_api_key_creden~",
                        column: x => x.api_key_credential_id,
                        principalSchema: "security",
                        principalTable: "api_key_credentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_key_credentials_client_revoked",
                schema: "security",
                table: "api_key_credentials",
                columns: ApiKeyCredentialClientRevokedIndexColumns);

            migrationBuilder.CreateIndex(
                name: "ix_api_key_credentials_expires_at",
                schema: "security",
                table: "api_key_credentials",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_api_key_credentials_replaces_credential_id",
                schema: "security",
                table: "api_key_credentials",
                column: "replaces_credential_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_api_key_credentials_key_id",
                schema: "security",
                table: "api_key_credentials",
                column: "key_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_key_lifecycle_events_credential_time",
                schema: "security",
                table: "api_key_lifecycle_events",
                columns: ApiKeyLifecycleEventCredentialTimeIndexColumns);

            migrationBuilder.CreateIndex(
                name: "ix_client_applications_organization_status",
                schema: "security",
                table: "client_applications",
                columns: ClientApplicationOrganizationStatusIndexColumns);

            migrationBuilder.CreateIndex(
                name: "ux_client_applications_client_identifier",
                schema: "security",
                table: "client_applications",
                column: "client_identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizations_status",
                schema: "security",
                table: "organizations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_organizations_normalized_code",
                schema: "security",
                table: "organizations",
                column: "normalized_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_key_credential_scopes",
                schema: "security");

            migrationBuilder.DropTable(
                name: "api_key_lifecycle_events",
                schema: "security");

            migrationBuilder.DropTable(
                name: "api_key_credentials",
                schema: "security");

            migrationBuilder.DropTable(
                name: "client_applications",
                schema: "security");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "security");
        }
    }
}
