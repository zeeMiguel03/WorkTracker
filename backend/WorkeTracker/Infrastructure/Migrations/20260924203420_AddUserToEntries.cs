using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "entries",
                type: "int",
                nullable: true);

            // Existing entries inherit their owner from the account that created them.
            migrationBuilder.Sql(@"
                UPDATE entries
                SET user_id = accounts.user_id
                FROM entries
                INNER JOIN accounts ON accounts.id = entries.account_id
                WHERE entries.user_id IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "entries",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_entries_user_id",
                table: "entries",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_entries_users_user_id",
                table: "entries",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_entries_users_user_id",
                table: "entries");

            migrationBuilder.DropIndex(
                name: "IX_entries_user_id",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "entries");
        }
    }
}
