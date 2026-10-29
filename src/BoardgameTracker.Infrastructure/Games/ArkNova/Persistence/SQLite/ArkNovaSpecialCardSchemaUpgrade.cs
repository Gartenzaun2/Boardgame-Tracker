using System.Data.Common;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.Seeding;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite;

/// <summary>One atomic, idempotent upgrade of the pre-reference-data card schema.</summary>
internal static class ArkNovaSpecialCardSchemaUpgrade
{
    public static async Task ApplyAsync(BoardgameTrackerDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            bool legacy = false;
            await using (DbCommand inspect = db.Database.GetDbConnection().CreateCommand())
            {
                inspect.CommandText = "PRAGMA table_info(ArkNovaGameSessionSpecialCardAssignments)";
                await using DbDataReader reader = await inspect.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (reader.GetString(1) == "SpecialCard")
                        legacy = true;
                }
            }
            if (!legacy)
                return;

            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            int unknown = await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM ArkNovaGameSessionSpecialCardAssignments WHERE SpecialCard NOT BETWEEN 1 AND 5")
                .SingleAsync(cancellationToken);
            if (unknown != 0)
                throw new InvalidOperationException("Unknown legacy special-card values; upgrade rolled back without deleting data.");

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "ArkNovaSpecialCards" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_ArkNovaSpecialCards" PRIMARY KEY,
                    "Name" TEXT NOT NULL,
                    "Symbol1" INTEGER NULL, "Symbol2" INTEGER NULL, "Symbol3" INTEGER NULL,
                    "IsArchived" INTEGER NOT NULL,
                    CONSTRAINT "CK_ArkNovaSpecialCards_Name" CHECK (length(trim(Name)) BETWEEN 1 AND 120),
                    CONSTRAINT "CK_ArkNovaSpecialCards_Symbols" CHECK (
                        (Symbol1 IS NULL OR Symbol1 BETWEEN 1 AND 14) AND
                        (Symbol2 IS NULL OR Symbol2 BETWEEN 1 AND 14) AND
                        (Symbol3 IS NULL OR Symbol3 BETWEEN 1 AND 14) AND
                        (Symbol2 IS NULL OR Symbol1 IS NOT NULL) AND
                        (Symbol3 IS NULL OR Symbol2 IS NOT NULL))
                );
                """, cancellationToken);
            foreach (ArkNovaSpecialCard card in ArkNovaSpecialCardDefaults.Create())
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO ArkNovaSpecialCards (Id, Name, Symbol1, Symbol2, Symbol3, IsArchived)
                    VALUES ({card.Id}, {card.Name}, {card.Symbol1}, {card.Symbol2}, {card.Symbol3}, {false})
                    ON CONFLICT(Id) DO NOTHING
                    """, cancellationToken);
            }
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE "ArkNovaGameSessionSpecialCardAssignments_New" (
                    "GameSessionId" TEXT NOT NULL, "PlayerId" TEXT NOT NULL, "SpecialCardId" TEXT NOT NULL,
                    CONSTRAINT "PK_ArkNovaGameSessionSpecialCardAssignments" PRIMARY KEY ("GameSessionId", "PlayerId", "SpecialCardId"),
                    CONSTRAINT "FK_Assignment_Player" FOREIGN KEY ("GameSessionId", "PlayerId")
                        REFERENCES "GameSessionPlayers" ("GameSessionId", "PlayerId") ON DELETE CASCADE,
                    CONSTRAINT "FK_Assignment_Card" FOREIGN KEY ("SpecialCardId")
                        REFERENCES "ArkNovaSpecialCards" ("Id") ON DELETE RESTRICT
                );
                """, cancellationToken);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO ArkNovaGameSessionSpecialCardAssignments_New (GameSessionId, PlayerId, SpecialCardId)
                SELECT GameSessionId, PlayerId, CASE SpecialCard
                    WHEN 1 THEN {ArkNovaSpecialCardDefaults.AfricanElephant}
                    WHEN 2 THEN {ArkNovaSpecialCardDefaults.AsianElephant}
                    WHEN 3 THEN {ArkNovaSpecialCardDefaults.BaldEagle}
                    WHEN 4 THEN {ArkNovaSpecialCardDefaults.GoldenEagle}
                    WHEN 5 THEN {ArkNovaSpecialCardDefaults.SunBear} END
                FROM ArkNovaGameSessionSpecialCardAssignments;
                """, cancellationToken);
            await db.Database.ExecuteSqlRawAsync("""
                DROP TABLE ArkNovaGameSessionSpecialCardAssignments;
                ALTER TABLE ArkNovaGameSessionSpecialCardAssignments_New RENAME TO ArkNovaGameSessionSpecialCardAssignments;
                CREATE INDEX IX_ArkNovaGameSessionSpecialCardAssignments_SpecialCardId
                    ON ArkNovaGameSessionSpecialCardAssignments (SpecialCardId);
                """, cancellationToken);
            await using (DbCommand check = db.Database.GetDbConnection().CreateCommand())
            {
                check.Transaction = transaction.GetDbTransaction();
                check.CommandText = "PRAGMA foreign_key_check(ArkNovaGameSessionSpecialCardAssignments)";
                await using DbDataReader reader = await check.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException("Special-card foreign-key validation failed; upgrade rolled back.");
            }
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
