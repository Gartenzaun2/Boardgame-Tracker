using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArkNovaZooPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    NameResourceKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IconKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArkNovaZooPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsTemporary = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsGuest = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSessions_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameSessions_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupPersons",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefaultOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPersons", x => new { x.GroupId, x.PersonId });
                    table.CheckConstraint("CK_GroupPersons_DefaultOrder_Positive", "DefaultOrder > 0");
                    table.ForeignKey(
                        name: "FK_GroupPersons_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupPersons_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameSessionPlayers",
                columns: table => new
                {
                    GameSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Placement = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessionPlayers", x => new { x.GameSessionId, x.PersonId });
                    table.CheckConstraint("CK_GameSessionPlayers_Order_Positive", "\"Order\" > 0");
                    table.CheckConstraint("CK_GameSessionPlayers_Placement_Positive", "Placement IS NULL OR Placement > 0");
                    table.ForeignKey(
                        name: "FK_GameSessionPlayers_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameSessionPlayers_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArkNovaGroupPlayers",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefaultColor = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArkNovaGroupPlayers", x => new { x.GroupId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_ArkNovaGroupPlayers_GroupPersons_GroupId_PersonId",
                        columns: x => new { x.GroupId, x.PersonId },
                        principalTable: "GroupPersons",
                        principalColumns: new[] { "GroupId", "PersonId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArkNovaSessionPlayers",
                columns: table => new
                {
                    GameSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: false),
                    ZooPlanOption1Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ZooPlanOption2Id = table.Column<int>(type: "INTEGER", nullable: false),
                    SelectedZooPlanId = table.Column<int>(type: "INTEGER", nullable: true),
                    Attractiveness = table.Column<int>(type: "INTEGER", nullable: true),
                    Conservation = table.Column<int>(type: "INTEGER", nullable: true),
                    Reputation = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArkNovaSessionPlayers", x => new { x.GameSessionId, x.PersonId });
                    table.CheckConstraint("CK_ArkNovaSessionPlayers_DifferentZooPlanOptions", "ZooPlanOption1Id <> ZooPlanOption2Id");
                    table.CheckConstraint("CK_ArkNovaSessionPlayers_SelectedZooPlanWasOffered", "SelectedZooPlanId IS NULL OR SelectedZooPlanId = ZooPlanOption1Id OR SelectedZooPlanId = ZooPlanOption2Id");
                    table.ForeignKey(
                        name: "FK_ArkNovaSessionPlayers_ArkNovaZooPlans_SelectedZooPlanId",
                        column: x => x.SelectedZooPlanId,
                        principalTable: "ArkNovaZooPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArkNovaSessionPlayers_ArkNovaZooPlans_ZooPlanOption1Id",
                        column: x => x.ZooPlanOption1Id,
                        principalTable: "ArkNovaZooPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArkNovaSessionPlayers_ArkNovaZooPlans_ZooPlanOption2Id",
                        column: x => x.ZooPlanOption2Id,
                        principalTable: "ArkNovaZooPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArkNovaSessionPlayers_GameSessionPlayers_GameSessionId_PersonId",
                        columns: x => new { x.GameSessionId, x.PersonId },
                        principalTable: "GameSessionPlayers",
                        principalColumns: new[] { "GameSessionId", "PersonId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArkNovaGroupPlayers_GroupId_DefaultColor",
                table: "ArkNovaGroupPlayers",
                columns: new[] { "GroupId", "DefaultColor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArkNovaSessionPlayers_GameSessionId_Color",
                table: "ArkNovaSessionPlayers",
                columns: new[] { "GameSessionId", "Color" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArkNovaSessionPlayers_SelectedZooPlanId",
                table: "ArkNovaSessionPlayers",
                column: "SelectedZooPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ArkNovaSessionPlayers_ZooPlanOption1Id",
                table: "ArkNovaSessionPlayers",
                column: "ZooPlanOption1Id");

            migrationBuilder.CreateIndex(
                name: "IX_ArkNovaSessionPlayers_ZooPlanOption2Id",
                table: "ArkNovaSessionPlayers",
                column: "ZooPlanOption2Id");

            migrationBuilder.CreateIndex(
                name: "IX_ArkNovaZooPlans_Number",
                table: "ArkNovaZooPlans",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_Key",
                table: "Games",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameSessionPlayers_GameSessionId_Order",
                table: "GameSessionPlayers",
                columns: new[] { "GameSessionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameSessionPlayers_PersonId",
                table: "GameSessionPlayers",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_GameId",
                table: "GameSessions",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_GroupId",
                table: "GameSessions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupPersons_GroupId_DefaultOrder",
                table: "GroupPersons",
                columns: new[] { "GroupId", "DefaultOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupPersons_PersonId",
                table: "GroupPersons",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArkNovaGroupPlayers");

            migrationBuilder.DropTable(
                name: "ArkNovaSessionPlayers");

            migrationBuilder.DropTable(
                name: "GroupPersons");

            migrationBuilder.DropTable(
                name: "ArkNovaZooPlans");

            migrationBuilder.DropTable(
                name: "GameSessionPlayers");

            migrationBuilder.DropTable(
                name: "GameSessions");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Groups");
        }
    }
}
