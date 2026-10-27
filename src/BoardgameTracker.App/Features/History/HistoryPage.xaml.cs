using BoardgameTracker.Application.History;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.Core.Games.ArkNova;
namespace BoardgameTracker.App.Features.History;

public partial class HistoryPage : ContentPage
{
    private readonly IHistoryQuery _query;
    private readonly IAppNavigationSettingsStore _settings;
    private IReadOnlyList<HistoryGameSession> _all = [];
    private string?[] _games = [];
    private Guid?[] _groups = [];
    private bool _loading;
    private bool _navigating;
    private bool _initialized;
    public HistoryPage(IHistoryQuery query, IAppNavigationSettingsStore settings)
    {
        InitializeComponent(); _query = query; _settings = settings;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loading) return;
        _loading = true; BusyOverlay.IsVisible = true; ErrorLabel.IsVisible = false;
        try
        {
            var game = GameFilter.SelectedIndex >= 0 ? _games[GameFilter.SelectedIndex] : null;
            var group = GroupFilter.SelectedIndex >= 0 ? _groups[GroupFilter.SelectedIndex] : null;
            _all = await _query.ListAsync();
            _games = new string?[] { null }.Concat(_all.Select(s => s.GameKey).Distinct().OrderBy(k => k)).ToArray();
            _groups = new Guid?[] { null }.Concat(_all.Select(s => (Guid?)s.GroupId).Distinct()).ToArray();
            if (!_initialized) game = _settings.Get().SelectedGameKey;
            GameFilter.ItemsSource = _games.Select(k => k is null ? AppResources.HistoryAllGames : GameName(k)).ToArray();
            GroupFilter.ItemsSource = _groups.Select(id => id is null ? AppResources.HistoryAllGroups : _all.First(s => s.GroupId == id).GroupName).ToArray();
            GameFilter.SelectedIndex = Math.Max(0, Array.IndexOf(_games, game));
            GroupFilter.SelectedIndex = Math.Max(0, Array.IndexOf(_groups, group));
            _initialized = true; Refresh();
        }
        catch (Exception) { ErrorLabel.Text = AppResources.HistoryLoadFailed; ErrorLabel.IsVisible = true; }
        finally { _loading = false; BusyOverlay.IsVisible = false; }
    }
    private static string GameName(string key) => key == ArkNovaGame.Key ? AppResources.ArkNova : key;
    private void OnFilterChanged(object? sender, EventArgs e) { if (!_loading) Refresh(); }
    private void Refresh()
    {
        var game = GameFilter.SelectedIndex >= 0 ? _games[GameFilter.SelectedIndex] : null;
        var group = GroupFilter.SelectedIndex >= 0 ? _groups[GroupFilter.SelectedIndex] : null;
        Sessions.ItemsSource = _all.Where(s => (game is null || s.GameKey == game) && (group is null || s.GroupId == group))
            .Select(s => new HistoryCard(s, GameName(s.GameKey))).ToArray();
    }
    private async void OnSessionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_navigating || e.CurrentSelection.FirstOrDefault() is not HistoryCard card) return;
        _navigating = true; Sessions.SelectedItem = null; BusyOverlay.IsVisible = true;
        try
        {
            if (card.Data.GameKey != ArkNovaGame.Key) throw new InvalidOperationException();
            await Shell.Current.GoToAsync(nameof(ArkNovaScoringPage), new ShellNavigationQueryParameters { [NavigationParameters.GameSessionId] = card.Data.GameSessionId });
        }
        catch (Exception) { ErrorLabel.Text = AppResources.HistoryOpenFailed; ErrorLabel.IsVisible = true; }
        finally { _navigating = false; BusyOverlay.IsVisible = false; }
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
            if (Data.EndedAt is null) return "–";
            int minutes = Math.Max(0, (int)(Data.EndedAt.Value - Data.StartedAt).TotalMinutes);
            return string.Format(AppResources.HistoryDurationFormat, minutes / 60, minutes % 60);
        }
    }
    public string Placements => string.Join(" · ", Data.Players.Select(p => $"{p.Placement}. {p.Name}"));
}
