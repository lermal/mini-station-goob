using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class PendingModelSync20260217 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_ghost_role_tickets",
                columns: table => new
                {
                    player_ghost_role_tickets_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tickets = table.Column<int>(type: "INTEGER", nullable: false),
                    last_grant_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ticket_milestones = table.Column<string>(type: "jsonb", nullable: false),
                    streak_milestones = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_ghost_role_tickets", x => x.player_ghost_role_tickets_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_ghost_role_tickets_player_id",
                table: "player_ghost_role_tickets",
                column: "player_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_player_ghost_role_tickets_player_id",
                table: "player_ghost_role_tickets");

            migrationBuilder.DropTable(
                name: "player_ghost_role_tickets");
        }
    }
}
