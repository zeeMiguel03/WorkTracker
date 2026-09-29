using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSalesAndDashboardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "products",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "product_sales",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    product_id = table.Column<int>(type: "int", nullable: true),
                    product_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    sale_source_id = table.Column<int>(type: "int", nullable: true),
                    sale_source_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    purchase_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    allocated_shipping_cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    allocated_other_costs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    sale_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    sale_other_costs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    sold_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sale_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_sales", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_sales_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_product_sales_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_user_id_status",
                table: "products",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_product_sales_product_id",
                table: "product_sales",
                column: "product_id",
                unique: true,
                filter: "[product_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_product_sales_user_id_sale_date_id",
                table: "product_sales",
                columns: new[] { "user_id", "sale_date", "id" });

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM products
                    WHERE (status = N'Sold' AND (sold_at IS NULL OR sale_price IS NULL))
                       OR (sold_at IS NULL AND sale_price IS NOT NULL)
                       OR (sold_at IS NOT NULL AND sale_price IS NULL)
                )
                BEGIN
                    THROW 50000, 'Existing product sales are incomplete; repair them before this migration.', 1;
                END;

                INSERT INTO product_sales (
                    user_id, product_id, product_name, sale_source_id, sale_source_name,
                    purchase_price, allocated_shipping_cost, allocated_other_costs,
                    sale_price, sale_other_costs, sold_at, sale_date
                )
                SELECT
                    p.user_id, p.id, p.name, p.sale_source_id, s.name,
                    p.purchase_price, p.allocated_shipping_cost, p.allocated_other_costs,
                    p.sale_price, COALESCE(p.sale_other_costs, 0), p.sold_at,
                    CONVERT(date, (p.sold_at AT TIME ZONE 'UTC') AT TIME ZONE 'GMT Standard Time')
                FROM products AS p
                LEFT JOIN sources AS s
                    ON s.id = p.sale_source_id AND s.user_id = p.user_id
                WHERE p.sold_at IS NOT NULL AND p.sale_price IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM product_sales)
                BEGIN
                    THROW 50001, 'Cannot roll back product_sales while sales exist.', 1;
                END;
                """);

            migrationBuilder.DropTable(
                name: "product_sales");

            migrationBuilder.DropIndex(
                name: "IX_products_user_id_status",
                table: "products");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "products");
        }
    }
}
