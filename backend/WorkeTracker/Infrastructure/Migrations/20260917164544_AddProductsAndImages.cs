using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductsAndImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    entry_id = table.Column<int>(type: "int", nullable: true),
                    source_id = table.Column<int>(type: "int", nullable: true),
                    tracking_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    shipping_cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    other_costs = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ordered_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_orders_entries_entry_id",
                        column: x => x.entry_id,
                        principalTable: "entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    purchase_order_id = table.Column<int>(type: "int", nullable: true),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    condition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    purchase_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    allocated_shipping_cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    allocated_other_costs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    listing_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    minimum_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    sale_entry_id = table.Column<int>(type: "int", nullable: true),
                    sale_source_id = table.Column<int>(type: "int", nullable: true),
                    sale_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    sale_other_costs = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    sold_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_products_entries_sale_entry_id",
                        column: x => x.sale_entry_id,
                        principalTable: "entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_sources_sale_source_id",
                        column: x => x.sale_source_id,
                        principalTable: "sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    is_cover = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_images_product_id_display_order",
                table: "product_images",
                columns: new[] { "product_id", "display_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_product_images_cover",
                table: "product_images",
                column: "product_id",
                unique: true,
                filter: "[is_cover] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_products_purchase_order_id",
                table: "products",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_sale_entry_id",
                table: "products",
                column: "sale_entry_id",
                unique: true,
                filter: "[sale_entry_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_products_sale_source_id",
                table: "products",
                column: "sale_source_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_user_id_created_at",
                table: "products",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_products_user_id_status",
                table: "products",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_entry_id",
                table: "purchase_orders",
                column: "entry_id",
                unique: true,
                filter: "[entry_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_source_id",
                table: "purchase_orders",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_user_id_status",
                table: "purchase_orders",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "purchase_orders");
        }
    }
}
