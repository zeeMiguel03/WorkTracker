using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPaginationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_user_id_status",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_products_user_id_created_at",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_user_id_status",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_user_id_created_at_id",
                table: "purchase_orders",
                columns: new[] { "user_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_user_id_status_created_at_id",
                table: "purchase_orders",
                columns: new[] { "user_id", "status", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_products_user_id_created_at_id",
                table: "products",
                columns: new[] { "user_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_products_user_id_status_created_at_id",
                table: "products",
                columns: new[] { "user_id", "status", "created_at", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_user_id_created_at_id",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_user_id_status_created_at_id",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_products_user_id_created_at_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_user_id_status_created_at_id",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_user_id_status",
                table: "purchase_orders",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_products_user_id_created_at",
                table: "products",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_products_user_id_status",
                table: "products",
                columns: new[] { "user_id", "status" });
        }
    }
}
