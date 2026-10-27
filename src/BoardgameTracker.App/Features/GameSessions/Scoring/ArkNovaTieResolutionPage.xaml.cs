using BoardgameTracker.App.Features.GameSessions.Result;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.GameSessions.Scoring;

/// <summary>
/// View-specific drag/drop rendering lives here; the chosen placements and validation remain in the view model/core rules.
/// </summary>
public partial class ArkNovaTieResolutionPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaTieResolutionViewModel _viewModel;
    private Guid _sessionId;
    private Guid _draggedPersonId;

    public ArkNovaTieResolutionPage(ArkNovaTieResolutionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ArkNovaTieResolutionViewModel.CanFinish)) RenderFrames();
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.SessionId, out object? value)
            || value is not Guid sessionId || sessionId == Guid.Empty)
        {
            throw new InvalidOperationException("Required session navigation parameter is missing or invalid.");
        }
        _sessionId = sessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_sessionId);
        RenderFrames();
    }

    private void RenderFrames()
    {
        if (FramesHost is null) return;
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
                    drag.DragStarting += (_, _) => _draggedPersonId = player.PersonId;
                    row.GestureRecognizers.Add(drag);
                }
                content.Children.Add(row);
            }

            if (frame.Players.Count == 0)
            {
                content.Children.Add(new Label { Text = "Hierher ziehen", FontSize = 11, Opacity = 0.55 });
            }

            Border border = new()
            {
                Padding = 8,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Stroke = (Color)Microsoft.Maui.Controls.Application.Current!.Resources["Outline"],
                Content = content
            };
            DropGestureRecognizer drop = new() { AllowDrop = true };
            drop.Drop += (_, _) =>
            {
                if (_draggedPersonId != Guid.Empty && _viewModel.CanMove(_draggedPersonId, frame.Placement))
                {
                    _viewModel.Move(_draggedPersonId, frame.Placement);
                    _draggedPersonId = Guid.Empty;
                    RenderFrames();
                }
            };
            border.GestureRecognizers.Add(drop);
            FramesHost.Children.Add(border);
        }
    }

    private async void OnFinishClicked(object? sender, EventArgs e)
    {
        await _viewModel.CompleteAsync();
        await Shell.Current.GoToAsync(
            nameof(ArkNovaResultPage),
            new ShellNavigationQueryParameters { [NavigationParameters.SessionId] = _sessionId });
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
