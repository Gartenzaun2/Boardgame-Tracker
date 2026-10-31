using BoardgameTracker.Application.Games.ArkNova.Content;
using BoardgameTracker.Application.Games.ArkNova.Persistence.Seeding;
using System.Text.Json;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.History.Transfer;

public sealed record ArkNovaTransferReferences(ArkNovaTransferCard[] SpecialCards);
public sealed record ArkNovaTransferCard(Guid Id, string? UserName, ArkNovaCardSymbol? Symbol1, ArkNovaCardSymbol? Symbol2, ArkNovaCardSymbol? Symbol3, bool IsArchived);
public sealed record ArkNovaTransferGameSession(ArkNovaTransferPlayer[] Players);
public sealed record ArkNovaTransferPlayer(Guid PlayerId, PlayerColor Color, int ZooPlanOption1Number, int ZooPlanOption2Number,
    int SelectedZooPlanNumber, int Attractiveness, int Conservation, int? Reputation, Guid[] SpecialCardIds);

public sealed class ArkNovaTransferSchema : IGameTransferSchema
{
    public string GameKey => ArkNovaGame.Key;
    public int SchemaVersion => 2;
    public static T Read<T>(JsonElement data) => data.Deserialize<T>(BgtFormat.JsonOptions) ?? throw new InvalidDataException("InvalidGameData");
    public void Validate(BgtGame game, IReadOnlyList<BgtGameSession> gameSessions)
    {
        ArkNovaTransferReferences references = Read<ArkNovaTransferReferences>(game.Data);
        BgtFormat.CheckIds(references.SpecialCards.Select(card => card.Id));
        HashSet<Guid> builtInIds = ArkNovaSpecialCardDefaults.Create().Select(card => card.Id).ToHashSet();
        foreach (ArkNovaTransferCard card in references.SpecialCards)
        {
            _ = new ArkNovaSpecialCard(card.Id, card.Symbol1, card.Symbol2, card.Symbol3);
            if (card.UserName is not null)
                _ = new ArkNovaUserContent(ArkNovaUserContent.SpecialCard, card.Id.ToString(), card.UserName);
            else if (!builtInIds.Contains(card.Id))
                throw new InvalidDataException("MissingUserCardName");
        }
        HashSet<Guid> cards = references.SpecialCards.Select(card => card.Id).ToHashSet();
        foreach (BgtGameSession gameSession in gameSessions)
        {
            ArkNovaTransferGameSession data = Read<ArkNovaTransferGameSession>(gameSession.Data);
            BgtFormat.CheckIds(data.Players.Select(player => player.PlayerId));
            if (data.Players.Length > 4 || !data.Players.Select(player => player.PlayerId).ToHashSet().SetEquals(gameSession.Players.Select(player => player.PlayerId))
                || data.Players.Select(player => player.Color).Distinct().Count() != data.Players.Length)
                throw new InvalidDataException("InvalidArkNovaPlayers");
            List<int> zooPlans = [];
            foreach (ArkNovaTransferPlayer player in data.Players)
            {
                ArkNovaGameSessionPlayer domain = new(gameSession.Id, player.PlayerId, player.Color,
                    player.ZooPlanOption1Number, player.ZooPlanOption2Number, player.SelectedZooPlanNumber);
                domain.SetScoreValues(player.Attractiveness, player.Conservation, player.Reputation);
                zooPlans.Add(player.ZooPlanOption1Number);
                zooPlans.Add(player.ZooPlanOption2Number);
                BgtFormat.CheckIds(player.SpecialCardIds);
                if (player.SpecialCardIds.Any(id => !cards.Contains(id)))
                    throw new InvalidDataException("MissingSpecialCard");
            }
            if (zooPlans.Any(number => number < 1 || number > 14) || zooPlans.Distinct().Count() != zooPlans.Count)
                throw new InvalidDataException("InvalidZooPlans");
            ArkNovaPlacementRules.ValidateResolution(data.Players.ToDictionary(player => player.PlayerId,
                player => ArkNovaScoringRules.CalculateNewScore(player.Attractiveness, player.Conservation)),
                gameSession.Players.ToDictionary(player => player.PlayerId, player => player.Placement));
        }
    }
}
