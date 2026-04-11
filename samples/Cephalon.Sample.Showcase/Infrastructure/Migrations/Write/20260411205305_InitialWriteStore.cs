using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cephalon.Sample.Showcase.Infrastructure.Migrations.Write
{
    /// <inheritdoc />
    public partial class InitialWriteStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cephalon_inbox_messages",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    channel_id = table.Column<string>(type: "text", nullable: false),
                    message_type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<string>(type: "text", nullable: true),
                    headers_json = table.Column<string>(type: "text", nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cephalon_inbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cephalon_outbox_messages",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    channel_id = table.Column<string>(type: "text", nullable: false),
                    message_type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<string>(type: "text", nullable: true),
                    headers_json = table.Column<string>(type: "text", nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dispatched_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dispatch_attempt_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cephalon_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "showcase_inventory",
                columns: table => new
                {
                    product_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    quantity_on_hand = table.Column<int>(type: "integer", nullable: false),
                    quantity_reserved = table.Column<int>(type: "integer", nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    last_updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_showcase_inventory", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "showcase_orders",
                columns: table => new
                {
                    order_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_in_cents = table.Column<long>(type: "bigint", nullable: false),
                    shipping_address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    placed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_showcase_orders", x => x.order_id);
                });

            migrationBuilder.CreateTable(
                name: "showcase_products",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    price_in_cents = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tags_json = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_showcase_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "showcase_shipments",
                columns: table => new
                {
                    shipment_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    order_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    destination_address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    carrier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tracking_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    estimated_delivery_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_showcase_shipments", x => x.shipment_id);
                });

            migrationBuilder.CreateTable(
                name: "showcase_order_line_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_in_cents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_showcase_order_line_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_showcase_order_line_items_showcase_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "showcase_orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cephalon_inbox_messages_channel_id",
                table: "cephalon_inbox_messages",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_cephalon_inbox_messages_processed_at_utc",
                table: "cephalon_inbox_messages",
                column: "processed_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_cephalon_outbox_messages_dispatched_at_utc",
                table: "cephalon_outbox_messages",
                column: "dispatched_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_cephalon_outbox_messages_next_attempt_at_utc",
                table: "cephalon_outbox_messages",
                column: "next_attempt_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_order_line_items_order_id",
                table: "showcase_order_line_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_orders_customer_id",
                table: "showcase_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_orders_status",
                table: "showcase_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_products_category",
                table: "showcase_products",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_products_sku",
                table: "showcase_products",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_showcase_shipments_order_id",
                table: "showcase_shipments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_showcase_shipments_status",
                table: "showcase_shipments",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cephalon_inbox_messages");

            migrationBuilder.DropTable(
                name: "cephalon_outbox_messages");

            migrationBuilder.DropTable(
                name: "showcase_inventory");

            migrationBuilder.DropTable(
                name: "showcase_order_line_items");

            migrationBuilder.DropTable(
                name: "showcase_products");

            migrationBuilder.DropTable(
                name: "showcase_shipments");

            migrationBuilder.DropTable(
                name: "showcase_orders");
        }
    }
}
