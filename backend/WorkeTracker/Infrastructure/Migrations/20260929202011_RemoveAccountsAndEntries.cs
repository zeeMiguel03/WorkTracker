using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAccountsAndEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_entries_sale_entry_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_entries_entry_id",
                table: "purchase_orders");

            migrationBuilder.DropTable(
                name: "entries");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "transaction_types");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_entry_id",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_products_sale_entry_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "entry_id",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "sale_entry_id",
                table: "products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "entry_id",
                table: "purchase_orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sale_entry_id",
                table: "products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    account_type = table.Column<int>(type: "int", nullable: false),
                    bank_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    card_brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    icon_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    include_in_total = table.Column<bool>(type: "bit", nullable: false),
                    initial_balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ut_creation = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_accounts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transaction_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ut_creation = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_types", x => x.id);
                    table.ForeignKey(
                        name: "FK_transaction_types_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entries",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    source_id = table.Column<int>(type: "int", nullable: false),
                    transaction_type_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ut_creation = table.Column<int>(type: "int", nullable: true),
                    value = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_entries_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_entries_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_entries_transaction_types_transaction_type_id",
                        column: x => x.transaction_type_id,
                        principalTable: "transaction_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_entry_id",
                table: "purchase_orders",
                column: "entry_id",
                unique: true,
                filter: "[entry_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_products_sale_entry_id",
                table: "products",
                column: "sale_entry_id",
                unique: true,
                filter: "[sale_entry_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_user_id_created_at",
                table: "accounts",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_user_id_name_id",
                table: "accounts",
                columns: new[] { "user_id", "name", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_entries_account_id_date",
                table: "entries",
                columns: new[] { "account_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_entries_account_id_source_id",
                table: "entries",
                columns: new[] { "account_id", "source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_entries_account_id_transaction_type_id",
                table: "entries",
                columns: new[] { "account_id", "transaction_type_id" });

            migrationBuilder.CreateIndex(
                name: "IX_entries_source_id",
                table: "entries",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_entries_transaction_type_id",
                table: "entries",
                column: "transaction_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_entries_user_id",
                table: "entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_types_user_id_name",
                table: "transaction_types",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_products_entries_sale_entry_id",
                table: "products",
                column: "sale_entry_id",
                principalTable: "entries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_entries_entry_id",
                table: "purchase_orders",
                column: "entry_id",
                principalTable: "entries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
