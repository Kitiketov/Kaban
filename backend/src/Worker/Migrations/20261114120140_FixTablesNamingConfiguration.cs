using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaban.Worker.Migrations
{
    /// <inheritdoc />
    public partial class FixTablesNamingConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_board_columns_boards_board_id",
                table: "board_columns");

            migrationBuilder.DropForeignKey(
                name: "FK_board_tags_boards_board_id",
                table: "board_tags");

            migrationBuilder.DropForeignKey(
                name: "FK_board_tags_task_tags_tag_id",
                table: "board_tags");

            migrationBuilder.DropForeignKey(
                name: "FK_boards_projects_project_id",
                table: "boards");

            migrationBuilder.DropForeignKey(
                name: "FK_roles_teams_team_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "FK_task_tags_tasks_task_id",
                table: "task_tags");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_board_columns_board_column_id",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_team_projects_projects_project_id",
                table: "team_projects");

            migrationBuilder.DropForeignKey(
                name: "FK_team_projects_teams_team_id",
                table: "team_projects");

            migrationBuilder.DropForeignKey(
                name: "FK_user_tasks_tasks_task_id",
                table: "user_tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_user_tasks_users_user_id",
                table: "user_tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_user_teams_roles_role_id",
                table: "user_teams");

            migrationBuilder.DropForeignKey(
                name: "FK_user_teams_teams_team_id",
                table: "user_teams");

            migrationBuilder.DropForeignKey(
                name: "FK_user_teams_users_user_id",
                table: "user_teams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_teams",
                table: "user_teams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_tasks",
                table: "user_tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_teams",
                table: "teams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_team_projects",
                table: "team_projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tasks",
                table: "tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_task_tags",
                table: "task_tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roles",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_projects",
                table: "projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_boards",
                table: "boards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_tags",
                table: "board_tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_board_columns",
                table: "board_columns");

            migrationBuilder.RenameIndex(
                name: "IX_users_email",
                table: "users",
                newName: "ix_users_email");

            migrationBuilder.RenameIndex(
                name: "IX_user_teams_user_id",
                table: "user_teams",
                newName: "ix_user_teams_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_teams_team_id",
                table: "user_teams",
                newName: "ix_user_teams_team_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_teams_role_id",
                table: "user_teams",
                newName: "ix_user_teams_role_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_tasks_user_id",
                table: "user_tasks",
                newName: "ix_user_tasks_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_tasks_task_id",
                table: "user_tasks",
                newName: "ix_user_tasks_task_id");

            migrationBuilder.RenameIndex(
                name: "IX_team_projects_team_id",
                table: "team_projects",
                newName: "ix_team_projects_team_id");

            migrationBuilder.RenameIndex(
                name: "IX_team_projects_project_id",
                table: "team_projects",
                newName: "ix_team_projects_project_id");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'tasks'
                          AND column_name = 'task_order'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'tasks'
                          AND column_name = 'task_rank'
                    ) THEN
                        ALTER TABLE tasks RENAME COLUMN task_order TO task_rank;
                    END IF;

                    IF to_regclass('"IX_tasks_board_column_id_task_rank"') IS NOT NULL THEN
                        ALTER INDEX "IX_tasks_board_column_id_task_rank"
                            RENAME TO ix_tasks_board_column_id_task_rank;
                    ELSIF to_regclass('"IX_tasks_board_column_id_task_order"') IS NOT NULL THEN
                        ALTER INDEX "IX_tasks_board_column_id_task_order"
                            RENAME TO ix_tasks_board_column_id_task_rank;
                    END IF;
                END $$;
                """);

            migrationBuilder.RenameIndex(
                name: "IX_tasks_board_column_id",
                table: "tasks",
                newName: "ix_tasks_board_column_id");

            migrationBuilder.RenameIndex(
                name: "IX_task_tags_task_id_tag",
                table: "task_tags",
                newName: "ix_task_tags_task_id_tag");

            migrationBuilder.RenameIndex(
                name: "IX_task_tags_task_id",
                table: "task_tags",
                newName: "ix_task_tags_task_id");

            migrationBuilder.RenameIndex(
                name: "IX_roles_team_id_role_name",
                table: "roles",
                newName: "ix_roles_team_id_role_name");

            migrationBuilder.RenameIndex(
                name: "IX_roles_team_id",
                table: "roles",
                newName: "ix_roles_team_id");

            migrationBuilder.RenameIndex(
                name: "IX_boards_project_id",
                table: "boards",
                newName: "ix_boards_project_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_tags_tag_id",
                table: "board_tags",
                newName: "ix_board_tags_tag_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_tags_board_id",
                table: "board_tags",
                newName: "ix_board_tags_board_id");

            migrationBuilder.RenameIndex(
                name: "IX_board_columns_board_id_board_order",
                table: "board_columns",
                newName: "ix_board_columns_board_id_board_order");

            migrationBuilder.RenameIndex(
                name: "IX_board_columns_board_id",
                table: "board_columns",
                newName: "ix_board_columns_board_id");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:task_priority.priority_enum", "no_priority,low,medium,high,very_high");

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_teams",
                table: "user_teams",
                columns: new[] { "user_id", "team_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_tasks",
                table: "user_tasks",
                columns: new[] { "user_id", "task_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_teams",
                table: "teams",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_team_projects",
                table: "team_projects",
                columns: new[] { "project_id", "team_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_tasks",
                table: "tasks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_task_tags",
                table: "task_tags",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_roles",
                table: "roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_projects",
                table: "projects",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_boards",
                table: "boards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_tags",
                table: "board_tags",
                columns: new[] { "tag_id", "board_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_board_columns",
                table: "board_columns",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_board_columns_boards_board_id",
                table: "board_columns",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_tags_boards_board_id",
                table: "board_tags",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_board_tags_task_tags_tag_id",
                table: "board_tags",
                column: "tag_id",
                principalTable: "task_tags",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_boards_projects_project_id",
                table: "boards",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_roles_teams_team_id",
                table: "roles",
                column: "team_id",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_task_tags_tasks_task_id",
                table: "task_tags",
                column: "task_id",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tasks_board_columns_board_column_id",
                table: "tasks",
                column: "board_column_id",
                principalTable: "board_columns",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_team_projects_projects_project_id",
                table: "team_projects",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_team_projects_teams_team_id",
                table: "team_projects",
                column: "team_id",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_tasks_tasks_task_id",
                table: "user_tasks",
                column: "task_id",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_tasks_users_user_id",
                table: "user_tasks",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_teams_roles_role_id",
                table: "user_teams",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_teams_teams_team_id",
                table: "user_teams",
                column: "team_id",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_teams_users_user_id",
                table: "user_teams",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_board_columns_boards_board_id",
                table: "board_columns");

            migrationBuilder.DropForeignKey(
                name: "fk_board_tags_boards_board_id",
                table: "board_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_board_tags_task_tags_tag_id",
                table: "board_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_boards_projects_project_id",
                table: "boards");

            migrationBuilder.DropForeignKey(
                name: "fk_roles_teams_team_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "fk_task_tags_tasks_task_id",
                table: "task_tags");

            migrationBuilder.DropForeignKey(
                name: "fk_tasks_board_columns_board_column_id",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "fk_team_projects_projects_project_id",
                table: "team_projects");

            migrationBuilder.DropForeignKey(
                name: "fk_team_projects_teams_team_id",
                table: "team_projects");

            migrationBuilder.DropForeignKey(
                name: "fk_user_tasks_tasks_task_id",
                table: "user_tasks");

            migrationBuilder.DropForeignKey(
                name: "fk_user_tasks_users_user_id",
                table: "user_tasks");

            migrationBuilder.DropForeignKey(
                name: "fk_user_teams_roles_role_id",
                table: "user_teams");

            migrationBuilder.DropForeignKey(
                name: "fk_user_teams_teams_team_id",
                table: "user_teams");

            migrationBuilder.DropForeignKey(
                name: "fk_user_teams_users_user_id",
                table: "user_teams");

            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_teams",
                table: "user_teams");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_tasks",
                table: "user_tasks");

            migrationBuilder.DropPrimaryKey(
                name: "pk_teams",
                table: "teams");

            migrationBuilder.DropPrimaryKey(
                name: "pk_team_projects",
                table: "team_projects");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tasks",
                table: "tasks");

            migrationBuilder.DropPrimaryKey(
                name: "pk_task_tags",
                table: "task_tags");

            migrationBuilder.DropPrimaryKey(
                name: "pk_roles",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_projects",
                table: "projects");

            migrationBuilder.DropPrimaryKey(
                name: "pk_boards",
                table: "boards");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_tags",
                table: "board_tags");

            migrationBuilder.DropPrimaryKey(
                name: "pk_board_columns",
                table: "board_columns");

            migrationBuilder.RenameIndex(
                name: "ix_users_email",
                table: "users",
                newName: "IX_users_email");

            migrationBuilder.RenameIndex(
                name: "ix_user_teams_user_id",
                table: "user_teams",
                newName: "IX_user_teams_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_teams_team_id",
                table: "user_teams",
                newName: "IX_user_teams_team_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_teams_role_id",
                table: "user_teams",
                newName: "IX_user_teams_role_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_tasks_user_id",
                table: "user_tasks",
                newName: "IX_user_tasks_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_tasks_task_id",
                table: "user_tasks",
                newName: "IX_user_tasks_task_id");

            migrationBuilder.RenameIndex(
                name: "ix_team_projects_team_id",
                table: "team_projects",
                newName: "IX_team_projects_team_id");

            migrationBuilder.RenameIndex(
                name: "ix_team_projects_project_id",
                table: "team_projects",
                newName: "IX_team_projects_project_id");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('ix_tasks_board_column_id_task_rank') IS NOT NULL THEN
                        ALTER INDEX ix_tasks_board_column_id_task_rank
                            RENAME TO "IX_tasks_board_column_id_task_rank";
                    END IF;
                END $$;
                """);

            migrationBuilder.RenameIndex(
                name: "ix_tasks_board_column_id",
                table: "tasks",
                newName: "IX_tasks_board_column_id");

            migrationBuilder.RenameIndex(
                name: "ix_task_tags_task_id_tag",
                table: "task_tags",
                newName: "IX_task_tags_task_id_tag");

            migrationBuilder.RenameIndex(
                name: "ix_task_tags_task_id",
                table: "task_tags",
                newName: "IX_task_tags_task_id");

            migrationBuilder.RenameIndex(
                name: "ix_roles_team_id_role_name",
                table: "roles",
                newName: "IX_roles_team_id_role_name");

            migrationBuilder.RenameIndex(
                name: "ix_roles_team_id",
                table: "roles",
                newName: "IX_roles_team_id");

            migrationBuilder.RenameIndex(
                name: "ix_boards_project_id",
                table: "boards",
                newName: "IX_boards_project_id");

            migrationBuilder.RenameIndex(
                name: "ix_board_tags_tag_id",
                table: "board_tags",
                newName: "IX_board_tags_tag_id");

            migrationBuilder.RenameIndex(
                name: "ix_board_tags_board_id",
                table: "board_tags",
                newName: "IX_board_tags_board_id");

            migrationBuilder.RenameIndex(
                name: "ix_board_columns_board_id_board_order",
                table: "board_columns",
                newName: "IX_board_columns_board_id_board_order");

            migrationBuilder.RenameIndex(
                name: "ix_board_columns_board_id",
                table: "board_columns",
                newName: "IX_board_columns_board_id");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:task_priority.priority_enum", "no_priority,low,medium,high,very_high");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_teams",
                table: "user_teams",
                columns: new[] { "user_id", "team_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_tasks",
                table: "user_tasks",
                columns: new[] { "user_id", "task_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_teams",
                table: "teams",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_team_projects",
                table: "team_projects",
                columns: new[] { "project_id", "team_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_tasks",
                table: "tasks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_task_tags",
                table: "task_tags",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roles",
                table: "roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_projects",
                table: "projects",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_boards",
                table: "boards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_tags",
                table: "board_tags",
                columns: new[] { "tag_id", "board_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_board_columns",
                table: "board_columns",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_board_columns_boards_board_id",
                table: "board_columns",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_tags_boards_board_id",
                table: "board_tags",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_board_tags_task_tags_tag_id",
                table: "board_tags",
                column: "tag_id",
                principalTable: "task_tags",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_boards_projects_project_id",
                table: "boards",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_roles_teams_team_id",
                table: "roles",
                column: "team_id",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_task_tags_tasks_task_id",
                table: "task_tags",
                column: "task_id",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_board_columns_board_column_id",
                table: "tasks",
                column: "board_column_id",
                principalTable: "board_columns",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_team_projects_projects_project_id",
                table: "team_projects",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_team_projects_teams_team_id",
                table: "team_projects",
                column: "team_id",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_tasks_tasks_task_id",
                table: "user_tasks",
                column: "task_id",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_tasks_users_user_id",
                table: "user_tasks",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_teams_roles_role_id",
                table: "user_teams",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_teams_teams_team_id",
                table: "user_teams",
                column: "team_id",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_teams_users_user_id",
                table: "user_teams",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
