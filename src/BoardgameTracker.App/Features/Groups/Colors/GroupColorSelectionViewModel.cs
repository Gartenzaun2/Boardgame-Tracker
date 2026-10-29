using System.Collections.ObjectModel;
using System.Windows.Input;
using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.App.Presentation.PlayerColors;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.Players;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardgameTracker.App.Features.Groups.Colors;

public partial class GroupColorSelectionViewModel(GamePlayerColorCatalog catalog, IEnumerable<GameColorPresentation> games) : ObservableObject
{
    private readonly IReadOnlyDictionary<string, GameColorPresentation> _games = games.ToDictionary(game => game.GameKey);
    private GroupPlayerItemViewModel? _target;
    private IReadOnlyList<GroupPlayerItemViewModel> _players = [];
    public string? GameKey { get; private set; }
    public bool HasColors => catalog.Get(GameKey).AllowedColors.Count > 0;
    public string Title => string.Format(AppResources.SetColorForGameFormat,
        GameKey is not null && _games.TryGetValue(GameKey, out var game) ? game.Name() : GameKey);
    public string PlayerName => _target?.Name ?? string.Empty;
    public ObservableCollection<GroupColorOption> Options { get; } = [];
    [ObservableProperty] public partial bool IsVisible { get; private set; }

    public bool Configure(string? gameKey)
    {
        if (GameKey == gameKey) return false;
        Close();
        GameKey = gameKey;
        OnPropertyChanged(nameof(HasColors));
        OnPropertyChanged(nameof(Title));
        return true;
    }

    public void Open(GroupPlayerItemViewModel player, IReadOnlyList<GroupPlayerItemViewModel> players)
    {
        if (!HasColors || !players.Contains(player)) return;
        _target = player;
        _players = players;
        Options.Clear();
        AddOption(null);
        foreach (var color in catalog.Get(GameKey).AllowedColors) AddOption(color);
        OnPropertyChanged(nameof(PlayerName));
        IsVisible = true;
    }

    private void AddOption(PlayerColor? color) => Options.Add(new(color,
        color is { } value ? PlayerColorPresentation.GetName(value) : AppResources.NoPlayerColor,
        color is { } swatch ? PlayerColorPresentation.GetBackground(swatch) : Microsoft.Maui.Graphics.Colors.Transparent,
        _target?.DefaultColor == color, new Command(() => Select(color))));

    private void Select(PlayerColor? color)
    {
        if (!IsVisible || _target is null || color is { } value && !catalog.Get(GameKey).Allows(value)) return;
        var owner = color is null ? null : _players.FirstOrDefault(player => !ReferenceEquals(player, _target) && player.DefaultColor == color);
        var previous = _target.DefaultColor;
        _target.DefaultColor = color;
        if (owner is not null) owner.DefaultColor = previous;
        Close();
    }

    [RelayCommand]
    public void Close()
    {
        IsVisible = false;
        _target = null;
        _players = [];
        Options.Clear();
    }
}

public sealed record GroupColorOption(PlayerColor? Color, string Name, Microsoft.Maui.Graphics.Color Swatch, bool IsSelected, ICommand ChooseCommand)
{
    public bool HasSwatch => Color is not null;
}
