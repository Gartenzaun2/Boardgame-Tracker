using System.Text.Json;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Application.Games.ArkNova.Content;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.History;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ArkNova.History.Transfer;

public sealed class ArkNovaTransferPersistence : IGameTransferPersistence
{
    public string GameKey => ArkNovaGame.Key;
    public int SchemaVersion => 2;
    public async Task<JsonElement> ReadSnapshotAsync(BoardgameTrackerDbContext db, CancellationToken cancellationToken)
    {
        var cards = await db.ArkNovaSpecialCards.AsNoTracking().OrderBy(card => card.Id)
            .Select(card => new { card.Id, card.Symbol1, card.Symbol2, card.Symbol3, card.IsArchived }).ToArrayAsync(cancellationToken);
        var names = await db.ArkNovaUserContent.AsNoTracking().Where(value => value.ResourceType == ArkNovaUserContent.SpecialCard)
            .OrderBy(value => value.ObjectId).Select(value => new { value.ObjectId, value.Name }).ToArrayAsync(cancellationToken);
        return JsonSerializer.SerializeToElement(new { Cards = cards, UserNames = names }, BgtFormat.JsonOptions);
    }
    public async Task<JsonElement> ExportReferencesAsync(BoardgameTrackerDbContext db, Guid[] gameSessionIds, CancellationToken cancellationToken)
    {
        Guid[] ids = await db.ArkNovaGameSessionSpecialCardAssignments.AsNoTracking().Where(assignment => gameSessionIds.Contains(assignment.GameSessionId))
            .Select(assignment => assignment.SpecialCardId).Distinct().ToArrayAsync(cancellationToken);
        var rows = await db.ArkNovaSpecialCards.AsNoTracking().Where(card => ids.Contains(card.Id))
            .Select(card => new { card.Id, card.Symbol1, card.Symbol2, card.Symbol3, card.IsArchived }).ToArrayAsync(cancellationToken);
        Dictionary<string, string> names = await db.ArkNovaUserContent.AsNoTracking()
            .Where(value => value.ResourceType == ArkNovaUserContent.SpecialCard)
            .ToDictionaryAsync(value => value.ObjectId, value => value.Name, cancellationToken);
        ArkNovaTransferCard[] cards = rows.Select(card => new ArkNovaTransferCard(card.Id,
            names.GetValueOrDefault(card.Id.ToString()), card.Symbol1, card.Symbol2, card.Symbol3, card.IsArchived)).ToArray();
        return JsonSerializer.SerializeToElement(new ArkNovaTransferReferences(cards), BgtFormat.JsonOptions);
    }
    public async Task<IReadOnlyDictionary<Guid, JsonElement>> ExportGameSessionsAsync(BoardgameTrackerDbContext db, Guid[] gameSessionIds, CancellationToken cancellationToken)
    {
        var players = await db.ArkNovaGameSessionPlayers.AsNoTracking().Where(player => gameSessionIds.Contains(player.GameSessionId))
            .Select(player => new
            {
                player.GameSessionId,
                player.PlayerId,
                player.Color,
                player.ZooPlanOption1Number,
                player.ZooPlanOption2Number,
                player.SelectedZooPlanNumber,
                player.Attractiveness,
                player.Conservation,
                player.Reputation
            }).ToArrayAsync(cancellationToken);
        var assignments = await db.ArkNovaGameSessionSpecialCardAssignments.AsNoTracking().Where(assignment => gameSessionIds.Contains(assignment.GameSessionId))
            .Select(assignment => new { assignment.GameSessionId, assignment.PlayerId, assignment.SpecialCardId }).ToArrayAsync(cancellationToken);
        return gameSessionIds.ToDictionary(gameSessionId => gameSessionId, gameSessionId => JsonSerializer.SerializeToElement(new ArkNovaTransferGameSession(players.Where(player => player.GameSessionId == gameSessionId).Select(player => new ArkNovaTransferPlayer(player.PlayerId,
            player.Color, player.ZooPlanOption1Number, player.ZooPlanOption2Number, player.SelectedZooPlanNumber,
            player.Attractiveness!.Value, player.Conservation!.Value, player.Reputation,
            assignments.Where(assignment => assignment.GameSessionId == gameSessionId && assignment.PlayerId == player.PlayerId).Select(assignment => assignment.SpecialCardId).ToArray())).ToArray()), BgtFormat.JsonOptions));
    }
    public async Task<(int Existing, int New)> PreviewAsync(BoardgameTrackerDbContext db, BgtGame game, IReadOnlyList<BgtGameSession> gameSessions, CancellationToken cancellationToken)
    {
        HashSet<Guid> needed = gameSessions.SelectMany(gameSession => ArkNovaTransferSchema.Read<ArkNovaTransferGameSession>(gameSession.Data).Players).SelectMany(player => player.SpecialCardIds).ToHashSet();
        Guid[] local = await db.ArkNovaSpecialCards.AsNoTracking().Select(card => card.Id).ToArrayAsync(cancellationToken);
        int existing = 0;
        int added = 0;
        foreach (ArkNovaTransferCard input in ArkNovaTransferSchema.Read<ArkNovaTransferReferences>(game.Data).SpecialCards.Where(card => needed.Contains(card.Id)))
        {
            if (local.Contains(input.Id))
                existing++;
            else
                added++;
        }
        return (existing, added);
    }
    public async Task ImportAsync(BoardgameTrackerDbContext db, BgtGame game, IReadOnlyList<BgtGameSession> gameSessions,
        IReadOnlyDictionary<Guid, Guid> players, CancellationToken cancellationToken)
    {
        ArkNovaTransferReferences references = ArkNovaTransferSchema.Read<ArkNovaTransferReferences>(game.Data);
        HashSet<Guid> needed = gameSessions.SelectMany(gameSession => ArkNovaTransferSchema.Read<ArkNovaTransferGameSession>(gameSession.Data).Players)
            .SelectMany(player => player.SpecialCardIds).ToHashSet();
        Guid[] local = await db.ArkNovaSpecialCards.AsNoTracking().Select(card => card.Id).ToArrayAsync(cancellationToken);
        HashSet<string> localNames = (await db.ArkNovaUserContent.AsNoTracking()
            .Where(value => value.ResourceType == ArkNovaUserContent.SpecialCard)
            .Select(value => value.ObjectId).ToArrayAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        foreach (ArkNovaTransferCard input in references.SpecialCards.Where(card => needed.Contains(card.Id)))
        {
            if (!local.Contains(input.Id))
            {
                ArkNovaSpecialCard card = new(input.Id, input.Symbol1, input.Symbol2, input.Symbol3);
                card.SetArchived(input.IsArchived);
                db.ArkNovaSpecialCards.Add(card);
            }
            // A shipped card row does not itself provide user content. Existing user names win.
            if (input.UserName is not null && !localNames.Contains(input.Id.ToString()))
                db.ArkNovaUserContent.Add(new(ArkNovaUserContent.SpecialCard, input.Id.ToString(), input.UserName));
        }
        foreach (BgtGameSession gameSession in gameSessions)
        {
            foreach (ArkNovaTransferPlayer input in ArkNovaTransferSchema.Read<ArkNovaTransferGameSession>(gameSession.Data).Players)
            {
                Guid playerId = players[input.PlayerId];
                ArkNovaGameSessionPlayer player = new(gameSession.Id, playerId, input.Color, input.ZooPlanOption1Number,
                    input.ZooPlanOption2Number, input.SelectedZooPlanNumber);
                player.SetScoreValues(input.Attractiveness, input.Conservation, input.Reputation);
                db.ArkNovaGameSessionPlayers.Add(player);
                foreach (Guid card in input.SpecialCardIds)
                    db.ArkNovaGameSessionSpecialCardAssignments.Add(new(gameSession.Id, playerId, card));
            }
        }
    }
}
