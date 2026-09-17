using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260916200000_AddAccountPaginationFields")]
public partial class AddAccountPaginationFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "initial_balance",
            table: "accounts",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<bool>(
            name: "include_in_total",
            table: "accounts",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateIndex(
            name: "IX_accounts_user_id_name_id",
            table: "accounts",
            columns: new[] { "user_id", "name", "id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_accounts_user_id_name_id",
            table: "accounts");

        migrationBuilder.DropColumn(
            name: "initial_balance",
            table: "accounts");

        migrationBuilder.DropColumn(
            name: "include_in_total",
            table: "accounts");
    }
}
