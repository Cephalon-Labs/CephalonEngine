using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cephalon.Sample.Showcase.Infrastructure.Migrations.History
{
    /// <inheritdoc />
    public partial class InitialAuditHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "showcase_audit_history",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    summary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    subject_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    subject_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    persisted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    actor_display_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    actor_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    actor_is_system = table.Column<bool>(type: "boolean", nullable: false),
                    outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    changes_json = table.Column<string>(type: "text", nullable: false),
                    tags_json = table.Column<string>(type: "text", nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_showcase_audit_history", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_showcase_audit_history_category",
                table: "showcase_audit_history",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_audit_history_correlation_id",
                table: "showcase_audit_history",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_audit_history_occurred_at_utc",
                table: "showcase_audit_history",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_audit_history_persisted_at_utc",
                table: "showcase_audit_history",
                column: "persisted_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_audit_history_subject_id",
                table: "showcase_audit_history",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_audit_history_subject_type",
                table: "showcase_audit_history",
                column: "subject_type");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_audit_history_tenant_id",
                table: "showcase_audit_history",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "showcase_audit_history");
        }
    }
}
