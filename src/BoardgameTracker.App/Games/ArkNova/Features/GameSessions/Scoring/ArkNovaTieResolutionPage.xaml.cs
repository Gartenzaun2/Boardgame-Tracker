using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using Microsoft.Extensions.Logging;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

/// <summary>
/// View-specific drag/drop rendering lives here; the chosen placements and validation remain in the view model/core rules.
/// </summary>
public partial class ArkNovaTieResolutionPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaTieResolutionViewModel _viewModel;
    private readonly ILogger<ArkNovaTieResolutionPage> _logger;
    private Guid _gameSessionId;
    private Guid _draggedPlayerId;

    public ArkNovaTieResolutionPage(ArkNovaTieResolutionViewModel viewModel, ILogger<ArkNovaTieResolutionPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ArkNovaTieResolutionViewModel.CanFinish))
            {
                RenderFrames();
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GameSessionId, out object? value)
            || value is not Guid gameSessionId || gameSessionId == Guid.Empty)
        {
            throw new InvalidOperationException("Required gameSession navigation parameter is missing or invalid.");
        }
        _viewModel.Draft = query.TryGetValue(NavigationParameters.CorrectionDraft, out object? draft) ? draft as ArkNovaCorrectionDraft : null;
        _gameSessionId = gameSessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        await _viewModel.LoadAsync(_gameSessionId);
        RenderFrames();
    }

    private void RenderFrames()
    {
        if (FramesHost is null)
        {
            return;
        }
        FramesHost.Children.Clear();
        foreach (ArkNovaPlacementFrameViewModel frame in _viewModel.Frames)
        {
            VerticalStackLayout content = new() { Spacing = 5 };
            content.Children.Add(new Label { Text = frame.Title, FontAttributes = FontAttributes.Bold, FontSize = 13 });

            foreach (ArkNovaPlacementPlayerViewModel player in frame.Players)
            {
                Grid row = new()
                {
                    Padding = new Thickness(10, 7),
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
                };
                row.Children.Add(new Label { Text = player.Name, FontAttributes = FontAttributes.Bold });
                Label score = new() { Text = player.ScoreText, FontSize = 12, VerticalOptions = LayoutOptions.Center };
                Grid.SetColumn(score, 1);
                row.Children.Add(score);

                if (player.IsMovable)
                {
                    DragGestureRecognizer drag = new();
                    drag.DragStarting += (_, _) => _draggedPlayerId = player.PlayerId;
                    row.GestureRecognizers.Add(drag);
                }
                content.Children.Add(row);
            }

            if (frame.Players.Count == 0)
            {
                Label hint = new() { Text = AppResources.DragHere, FontSize = 11 };
                hint.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
                content.Children.Add(hint);
            }

            Border border = new()
            {
                Padding = 8,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },

                Content = content
            };
            border.SetDynamicResource(Border.StrokeProperty, "Outline");
            border.SetDynamicResource(Border.BackgroundColorProperty, "Surface");
            DropGestureRecognizer drop = new() { AllowDrop = true };
            drop.Drop += (_, _) =>
            {
                if (_draggedPlayerId != Guid.Empty && _viewModel.CanMove(_draggedPlayerId, frame.Placement))
                {
                    _viewModel.Move(_draggedPlayerId, frame.Placement);
                    _draggedPlayerId = Guid.Empty;
                    RenderFrames();
                }
            };
            border.GestureRecognizers.Add(drop);
            FramesHost.Children.Add(border);
        }
    }

    private bool _saving;
    private async void OnFinishClicked(object? sender, EventArgs e)
    {
        if (_saving)
        {
            return;
        }
        _saving = true;
        try
        {
            await _viewModel.CompleteAsync();
            if (_viewModel.Draft is not null)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }
            await Shell.Current.GoToAsync(
                nameof(ArkNovaResultPage),
                new ShellNavigationQueryParameters { [NavigationParameters.GameSessionId] = _gameSessionId });
        }

        catch (Exception exception)
        {
            _logger.LogError(exception, "Ark Nova scoring operation failed for GameSession {GameSessionId}.", _gameSessionId);
            await AppDialogPage.ShowAsync(this, AppResources.Scoring, AppResources.CorrectionFailed, AppResources.Ok);
        }
        finally
        {
            _saving = false;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
