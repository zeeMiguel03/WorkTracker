using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToTransactionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transaction_types_name",
                table: "transaction_types");

            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "transaction_types",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE transaction_types
                SET user_id = ut_creation
                WHERE ut_creation IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM users
                      WHERE users.id = transaction_types.ut_creation
                  );

                IF EXISTS (SELECT 1 FROM transaction_types WHERE user_id IS NULL)
                    THROW 51000, 'Existing transaction types must have a valid ut_creation user before this migration can be applied.', 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "transaction_types",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_types_user_id_name",
                table: "transaction_types",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_transaction_types_users_user_id",
                table: "transaction_types",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transaction_types_users_user_id",
                table: "transaction_types");

            migrationBuilder.DropIndex(
                name: "IX_transaction_types_user_id_name",
                table: "transaction_types");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "transaction_types");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_types_name",
                table: "transaction_types",
                column: "name",
                unique: true);
        }
    }
}
