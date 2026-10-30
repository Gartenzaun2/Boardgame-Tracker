using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.App.Presentation.Controls;
using System.Globalization;
using Microsoft.Extensions.Logging;
using BoardgameTracker.Application.History;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;


namespace BoardgameTracker.App.Features.History;

public partial class HistoryPage : ContentPage
{
    private readonly IHistoryQuery _query;
    private readonly HistoryTransferUseCase _transfer;
    private Guid[] _visibleIds = [];
    private bool _transferring;
    private readonly ILogger<HistoryPage> _logger;
    private readonly IAppNavigationSettingsStore _settings;
    private readonly IEnumerable<IGameHistoryHandler> _historyHandlers;
    private IReadOnlyList<HistoryGameSession> _all = [];
    private string?[] _games = [];
    private Guid?[] _groups = [];
    private bool _loading;
    private bool _navigating;
    private bool _initialized;
    private bool _choosing;
    private int _gameIndex;
    private int _groupIndex;

    public HistoryPage(IHistoryQuery query, IAppNavigationSettingsStore settings, ILogger<HistoryPage> logger, IEnumerable<IGameHistoryHandler> historyHandlers, HistoryTransferUseCase transfer)
    {
        InitializeComponent();
        ExportButton.Text = string.Format(AppResources.TransferExportMany, 0);
        _query = query;
        _transfer = transfer;
        _settings = settings;
        _logger = logger;
        _historyHandlers = historyHandlers;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (_transferring || AppDialogPage.IsOpen(this))
            return;
        if (_loading)
        {
            return;
        }
        _loading = true;
        BusyOverlay.IsVisible = true;
        ErrorLabel.IsVisible = false;

        try
        {
            string? game = _games.Length > 0 ? _games[_gameIndex] : null;
            Guid? group = _groups.Length > 0 ? _groups[_groupIndex] : null;
            _all = await _query.ListAsync();
            _games = new string?[] { null }.Concat(_all.Select(s => s.GameKey).Distinct().OrderBy(k => k)).ToArray();
            _groups = new Guid?[] { null }.Concat(_all.Select(s => (Guid?)s.GroupId).Distinct()).ToArray();
            if (!_initialized)
            {
                game = _settings.Get().SelectedGameKey;
            }

            _gameIndex = Math.Max(0, Array.IndexOf(_games, game));
            _groupIndex = Math.Max(0, Array.IndexOf(_groups, group));
            _initialized = true;
            Refresh();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not load completed game sessions.");
            ErrorLabel.Text = AppResources.HistoryLoadFailed;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            _loading = false;
            BusyOverlay.IsVisible = false;
        }
    }

    private string GameName(string key) => _historyHandlers.FirstOrDefault(handler => handler.GameKey == key)?.DisplayName ?? key;

    private async void OnGameFilterClicked(object? sender, EventArgs e) => await ChooseFilterAsync(true);
    private async void OnGroupFilterClicked(object? sender, EventArgs e) => await ChooseFilterAsync(false);

    private async Task ChooseFilterAsync(bool gameFilter)
    {
        if (_transferring || _loading || _choosing || _games.Length == 0 || _groups.Length == 0)
            return;

        _choosing = true;
        try
        {
            string[] names = gameFilter
                ? _games.Select(key => key is null ? AppResources.HistoryAllGames : GameName(key)).ToArray()
                : _groups.Select(id => id is null ? AppResources.HistoryAllGroups : _all.First(session => session.GroupId == id).GroupName).ToArray();
            int choice = await AppDialogPage.ChooseAsync(this,
                gameFilter ? AppResources.HistoryGameFilter : AppResources.HistoryGroupFilter,
                names, gameFilter ? _gameIndex : _groupIndex);
            if (choice < 0)
                return;

            if (gameFilter)
                _gameIndex = choice;
            else
                _groupIndex = choice;
            Refresh();
        }
        finally
        {
            _choosing = false;
        }
    }
    private static void UpdateChip(Button chip, string name, bool selected)
    {
        chip.Text = string.Format(selected ? AppResources.HistoryFilterSelectedFormat : AppResources.HistoryFilterFormat, name);
        chip.SetDynamicResource(Button.BackgroundColorProperty, selected ? "SelectedSurface" : "Surface");
        chip.SetDynamicResource(Button.BorderColorProperty, selected ? "SelectedBorder" : "Outline");
        SemanticProperties.SetDescription(chip, chip.Text);
    }

    private void Refresh()
    {
        string? game = _games.Length > 0 ? _games[_gameIndex] : null;
        Guid? group = _groups.Length > 0 ? _groups[_groupIndex] : null;
        UpdateChip(GameChip, game is null ? AppResources.HistoryAllGames : GameName(game), game is not null);
        UpdateChip(GroupChip, group is null ? AppResources.HistoryAllGroups : _all.First(s => s.GroupId == group).GroupName, group is not null);

        IEnumerable<HistoryCard> cards = _all
            .Where(session => (game is null || session.GameKey == game) && (group is null || session.GroupId == group))
            .Select(session => new HistoryCard(session, GameName(session.GameKey)));
        HistoryCard[] visible = cards.ToArray();
        _visibleIds = visible.Select(card => card.Data.GameSessionId).ToArray();
        ExportButton.Text = string.Format(_visibleIds.Length == 1 ? AppResources.TransferExportOne : AppResources.TransferExportMany, _visibleIds.Length);
        ExportButton.IsEnabled = _visibleIds.Length > 0;
        var months = visible.GroupBy(card => (card.Data.StartedAt.ToLocalTime().Year, card.Data.StartedAt.ToLocalTime().Month));

        Sessions.ItemsSource = months.Select(month => new HistoryMonth(
            new DateTime(month.Key.Year, month.Key.Month, 1).ToString("MMMM yyyy", CultureInfo.CurrentCulture),
            month)).ToArray();
    }

    private async void OnSessionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_transferring || _loading || _navigating || e.CurrentSelection.FirstOrDefault() is not HistoryCard card)
        {
            return;
        }
        _navigating = true;
        Sessions.SelectedItem = null;
        BusyOverlay.IsVisible = true;

        try
        {
            IGameHistoryHandler handler = _historyHandlers.FirstOrDefault(handler => handler.GameKey == card.Data.GameKey)
                ?? throw new InvalidOperationException("No history handler is registered for this game.");
            await handler.OpenAsync(card.Data.GameSessionId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not open historical GameSession {GameSessionId}.", card.Data.GameSessionId);
            ErrorLabel.Text = AppResources.HistoryOpenFailed;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            _navigating = false;
            BusyOverlay.IsVisible = false;
        }
    }
}

public sealed record HistoryCard(HistoryGameSession Data, string GameName)
{
    public string GroupName => Data.GroupName;
    public string Header => string.Format(AppResources.HistoryHeaderFormat, GameName,
        Duration, Data.StartedAt.ToLocalTime().ToString("dd.MM.yy"));
    public string Duration
    {
        get
        {
            if (Data.EndedAt is null)
            {
                return "–";
            }
            int minutes = Math.Max(0, (int)(Data.EndedAt.Value - Data.StartedAt).TotalMinutes);
            return string.Format(AppResources.HistoryDurationFormat, minutes / 60, minutes % 60);
        }
    }
    public string Placements => string.Join(" · ", Data.Players.Select(p => $"{p.Placement}. {p.Name}"));
}

public sealed class HistoryMonth(string title, IEnumerable<HistoryCard> cards) : List<HistoryCard>(cards)
{
    public string Title { get; } = title;
}
