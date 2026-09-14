using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Tokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_accounts_account_types_account_type_id",
                table: "accounts");

            migrationBuilder.DropTable(
                name: "account_types");

            migrationBuilder.DropIndex(
                name: "IX_accounts_account_type_id",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "accounts");

            migrationBuilder.RenameColumn(
                name: "account_type_id",
                table: "accounts",
                newName: "account_type");

            migrationBuilder.AddColumn<string>(
                name: "icon_key",
                table: "accounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "icon_key",
                table: "accounts");

            migrationBuilder.RenameColumn(
                name: "account_type",
                table: "accounts",
                newName: "account_type_id");

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "accounts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "account_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ut_creation = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_account_type_id",
                table: "accounts",
                column: "account_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_types_name",
                table: "account_types",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_accounts_account_types_account_type_id",
                table: "accounts",
                column: "account_type_id",
                principalTable: "account_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
