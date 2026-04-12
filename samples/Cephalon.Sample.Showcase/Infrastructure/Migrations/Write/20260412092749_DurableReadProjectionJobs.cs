using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cephalon.Sample.Showcase.Infrastructure.Migrations.Write
{
    /// <inheritdoc />
    public partial class DurableReadProjectionJobs : Migration
    {
        private static readonly string[] PendingJobAvailabilityIndexColumns = ["completed_at_utc", "available_at_utc"];
        private static readonly string[] PendingJobLookupIndexColumns = ["scope", "entity_key", "completed_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "showcase_read_projection_jobs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entity_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_attempt_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    available_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_showcase_read_projection_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_showcase_read_projection_jobs_completed_at_utc_available_at~",
                table: "showcase_read_projection_jobs",
                columns: PendingJobAvailabilityIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_showcase_read_projection_jobs_scope_entity_key_completed_at~",
                table: "showcase_read_projection_jobs",
                columns: PendingJobLookupIndexColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "showcase_read_projection_jobs");
        }
    }
}
