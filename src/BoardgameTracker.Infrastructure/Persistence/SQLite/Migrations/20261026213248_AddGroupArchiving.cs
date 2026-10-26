using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Groups",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Groups");
        }
    }
}
