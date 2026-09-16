using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddDefaultTaskStatuses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO dbo.task_status (user_id, name, color, sort_order, created_at, ut_creation)
            SELECT users.id, defaults.name, defaults.color, defaults.sort_order, SYSUTCDATETIME(), users.id
            FROM dbo.users AS users
            CROSS JOIN (VALUES
                (N'A fazer', '#98A2B3', 0),
                (N'Em progresso', '#7592FF', 1),
                (N'Em revisão', '#F79009', 2),
                (N'Concluídas', '#12B76A', 3)
            ) AS defaults(name, color, sort_order)
            WHERE NOT EXISTS (
                SELECT 1
                FROM dbo.task_status AS existing_status
                WHERE existing_status.user_id = users.id
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately left empty so a rollback cannot delete user-customized columns.
    }
}
