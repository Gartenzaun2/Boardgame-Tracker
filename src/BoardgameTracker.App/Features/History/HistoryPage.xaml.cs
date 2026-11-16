using BoardgameTracker.Application.Games;
using BoardgameTracker.App.Features.History.Export;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.App.Presentation.Controls;
using System.Globalization;
using Microsoft.Extensions.Logging;
using BoardgameTracker.Application.History;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.History;

/// <summary>
/// 1. Load the complete export source and derive the enabled-game history and filter choices.
/// 2. Present one filter or transfer choice at a time; cancellation leaves persisted data unchanged.
/// 3. Resolve import identities and preview the plan before explicit atomic commit. Export uses
///    the complete filtered source, including hidden games; native picker and modal mechanics stay here.
/// </summary>
public partial class HistoryPage : ContentPage {
    private readonly CompletedGameSessionsQuery _query;
    private readonly GamesQuery _gamesQuery;
    private IReadOnlyList<HistoryGameSession> _exportSessions = [];
    private readonly HistoryTransferUseCase _transfer;
    private readonly HistoryExportFlow _exportFlow;
    private Guid[] _visibleIds = [];
    private bool _isTransferring;
    private readonly ILogger<HistoryPage> _logger;
    private readonly IAppNavigationSettingsStore _settings;
    private readonly IEnumerable<IGameSessionNavigationHandler> _navigationHandlers;
    /// <summary>The visible history excludes disabled games; export deliberately retains its separate complete source.</summary>
    private IReadOnlyList<HistoryGameSession> _visibleSessions = [];
    private string?[] _gameFilterKeys = [];
    private Guid?[] _groupFilterIds = [];
    private bool _isLoading;
    private bool _isNavigating;
    /// <summary>Apply the remembered game only once; later reloads preserve the user's filter selection.</summary>
    private bool _isInitialFilterSelectionApplied;
    private bool _isFilterSelectionOpen;
    private int _selectedGameIndex;
    private int _selectedGroupIndex;

    public HistoryPage(CompletedGameSessionsQuery query, IAppNavigationSettingsStore settings, ILogger<HistoryPage> logger, IEnumerable<IGameSessionNavigationHandler> navigationHandlers, HistoryTransferUseCase transfer, HistoryExportFlow exportFlow, GamesQuery gamesQuery) {
        InitializeComponent();
        ExportButton.Text = string.Format(AppResources.TransferExportMany, 0);
        _query = query;
        _gamesQuery = gamesQuery;
        _transfer = transfer;
        _exportFlow = exportFlow;
        _settings = settings;
        _logger = logger;
        _navigationHandlers = navigationHandlers;
    }

    protected override async void OnAppearing() {
        base.OnAppearing();
        await ReloadAsync();
    }

    private async Task ReloadAsync() {
        if (_isTransferring || _isNavigating || _isFilterSelectionOpen || AppDialogPage.IsOpen(this))
            return;
        if (_isLoading)
            return;
        _isLoading = true;
        BusyOverlay.IsVisible = true;
        ErrorLabel.IsVisible = false;

        try {
            string? game = _gameFilterKeys.Length > 0 ? _gameFilterKeys[_selectedGameIndex] : null;
            Guid? group = _groupFilterIds.Length > 0 ? _groupFilterIds[_selectedGroupIndex] : null;
            _exportSessions = await _query.ListAsync();
            HashSet<string> enabledGameKeys = (await _gamesQuery.GetEnabledAsync()).Select(game => game.Key).ToHashSet(StringComparer.Ordinal);
            _visibleSessions = _exportSessions.Where(session => enabledGameKeys.Contains(session.GameKey)).ToArray();
            _gameFilterKeys = new string?[] { null }.Concat(_visibleSessions.Select(s => s.GameKey).Distinct().OrderBy(k => k)).ToArray();
            _groupFilterIds = new Guid?[] { null }.Concat(_visibleSessions.Select(s => (Guid?)s.GroupId).Distinct()).ToArray();
            if (!_isInitialFilterSelectionApplied) {
                game = _settings.Get().SelectedGameKey;
            }

            _selectedGameIndex = Math.Max(0, Array.IndexOf(_gameFilterKeys, game));
            _selectedGroupIndex = Math.Max(0, Array.IndexOf(_groupFilterIds, group));
            _isInitialFilterSelectionApplied = true;
            Refresh();
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not load completed game sessions.");
            ErrorLabel.Text = AppResources.HistoryLoadFailed;
            ErrorLabel.IsVisible = true;
        } finally {
            _isLoading = false;
            BusyOverlay.IsVisible = false;
        }
    }

    private string GameName(string key)
        => _exportSessions.First(session => session.GameKey == key).GameName;

    private async void OnGameFilterClicked(object? sender, EventArgs e)
        => await ChooseFilterAsync(true);
    private async void OnGroupFilterClicked(object? sender, EventArgs e)
        => await ChooseFilterAsync(false);

    private async Task ChooseFilterAsync(bool gameFilter) {
        if (_isTransferring || _isLoading || _isNavigating || _isFilterSelectionOpen || _gameFilterKeys.Length == 0 || _groupFilterIds.Length == 0)
            return;

        _isFilterSelectionOpen = true;
        try {
            string[] names;
            if (gameFilter)
                names = _gameFilterKeys.Select(key => key is null ? AppResources.HistoryAllGames : GameName(key)).ToArray();
            else
                names = _groupFilterIds.Select(id => id is null ? AppResources.HistoryAllGroups : _visibleSessions.First(session => session.GroupId == id).GroupName).ToArray();
            int choice = await AppDialogPage.ChooseAsync(this,
                gameFilter ? AppResources.HistoryGameFilter : AppResources.HistoryGroupFilter,
                names, gameFilter ? _selectedGameIndex : _selectedGroupIndex);
            if (choice < 0)
                return;

            if (gameFilter)
                _selectedGameIndex = choice;
            else
                _selectedGroupIndex = choice;
            Refresh();
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not choose a history filter.");
            ErrorLabel.Text = AppResources.NavigationFailed;
            ErrorLabel.IsVisible = true;
        } finally {
            _isFilterSelectionOpen = false;
        }
    }
    private static void UpdateChip(Button chip, string name, bool selected) {
        chip.Text = string.Format(selected ? AppResources.HistoryFilterSelectedFormat : AppResources.HistoryFilterFormat, name);
        chip.SetDynamicResource(Button.BackgroundColorProperty, selected ? "SelectedSurface" : "Surface");
        chip.SetDynamicResource(Button.BorderColorProperty, selected ? "SelectedBorder" : "Outline");
        SemanticProperties.SetDescription(chip, chip.Text);
    }

    private void Refresh() {
        string? game = _gameFilterKeys.Length > 0 ? _gameFilterKeys[_selectedGameIndex] : null;
        Guid? group = _groupFilterIds.Length > 0 ? _groupFilterIds[_selectedGroupIndex] : null;
        UpdateChip(GameChip, game is null ? AppResources.HistoryAllGames : GameName(game), game is not null);
        UpdateChip(GroupChip, group is null ? AppResources.HistoryAllGroups : _visibleSessions.First(s => s.GroupId == group).GroupName, group is not null);

        IEnumerable<HistoryCard> cards = _visibleSessions
            .Where(session => (game is null || session.GameKey == game) && (group is null || session.GroupId == group))
            .Select(session => new HistoryCard(session, GameName(session.GameKey)));
        HistoryCard[] visible = cards.ToArray();
        _visibleIds = _exportSessions.Where(session => (game is null || session.GameKey == game) && (group is null || session.GroupId == group))
            .Select(session => session.GameSessionId).ToArray();
        ExportScopeHint.IsVisible = _visibleIds.Length > visible.Length;
        ExportButton.Text = string.Format(_visibleIds.Length == 1 ? AppResources.TransferExportOne : AppResources.TransferExportMany, _visibleIds.Length);
        ExportButton.IsEnabled = HistoryExportFlow.CanExport(_visibleIds);
        var months = visible.GroupBy(card => (card.Data.StartedAt.ToLocalTime().Year, card.Data.StartedAt.ToLocalTime().Month));

        Sessions.ItemsSource = months.Select(month => new HistoryMonth(
            new DateTime(month.Key.Year, month.Key.Month, 1).ToString("MMMM yyyy", CultureInfo.CurrentCulture),
            month)).ToArray();
    }

    private async void OnSessionSelected(object? sender, SelectionChangedEventArgs e) {
        if (_isTransferring || _isLoading || _isFilterSelectionOpen || _isNavigating || e.CurrentSelection.FirstOrDefault() is not HistoryCard card)
            return;
        _isNavigating = true;
        Sessions.SelectedItem = null;
        BusyOverlay.IsVisible = true;

        try {
            IGameSessionNavigationHandler handler = _navigationHandlers.FirstOrDefault(handler => handler.CanHandle(card.Data.GameKey))
                ?? throw new InvalidOperationException("No history handler is registered for this game.");
            await handler.OpenGameSessionAsync(card.Data.GameSessionId);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not open historical GameSession {GameSessionId}.", card.Data.GameSessionId);
            ErrorLabel.Text = AppResources.HistoryOpenFailed;
            ErrorLabel.IsVisible = true;
        } finally {
            _isNavigating = false;
            BusyOverlay.IsVisible = false;
        }
    }
}

public sealed record HistoryCard(HistoryGameSession Data, string GameName) {
    public string GroupName => Data.GroupName;
    public string Header => string.Format(AppResources.HistoryHeaderFormat, GameName,
        Duration, Data.StartedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
    public string Duration {
        get {
            if (Data.EndedAt is null)
                return "–";
            int minutes = Math.Max(0, (int)(Data.EndedAt.Value - Data.StartedAt).TotalMinutes);
            return string.Format(AppResources.HistoryDurationFormat, minutes / 60, minutes % 60);
        }
    }
    public string Placements => string.Join(" · ", Data.Players.Select(p =>
        p.Placement is int rank ? $"{rank}. {p.Name}" : p.Name));
}

public sealed class HistoryMonth(string title, IEnumerable<HistoryCard> cards) : List<HistoryCard>(cards) {
    public string Title { get; } = title;
}
