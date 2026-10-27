using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

/// <summary>
/// Shell root page for selecting, creating and editing groups. Concrete navigation
/// stays in the view while navigation re-entry state is held by the view model.
/// </summary>
public partial class GroupListPage : ContentPage
{
    private readonly GroupListViewModel _viewModel;

    public GroupListPage(GroupListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.EndNavigation();
        await _viewModel.LoadAsync();
    }

    private async void OnCreateGroupClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.TryBeginNavigation())
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync(nameof(CreateGroupPage));
        }
        catch
        {
            _viewModel.EndNavigation();
            throw;
        }
    }

    private async void OnGroupTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Guid groupId || !_viewModel.TryBeginNavigation())
        {
            return;
        }

        GroupListItemViewModel? group = _viewModel.Groups.FirstOrDefault(candidate => candidate.Id == groupId);

        if (group?.IsArchived == true)
        {
            _viewModel.EndNavigation();
            return;
        }

        if (_viewModel.SelectedGameKey != ArkNovaGame.Key)
        {
            _viewModel.EndNavigation();
            return;
        }

        try
        {
            if (group?.OpenGameSessionId is Guid gameSessionId)
            {
                string route = group.OpenGameSessionStatus switch
                {
                    GameSessionStatus.Running => nameof(ArkNovaRunningPage),
                    GameSessionStatus.Scoring => nameof(ArkNovaScoringPage),
                    _ => throw new InvalidOperationException(
                        $"Unsupported open GameSession status '{group.OpenGameSessionStatus}'.")
                };

                if (Shell.Current is AppShell shell)
                {
                    shell.RememberGameSession(gameSessionId, ArkNovaGame.Key);
                }

                await Shell.Current.GoToAsync(
                    route,
                    new ShellNavigationQueryParameters
                    {
                        [NavigationParameters.GameSessionId] = gameSessionId
                    });
                return;
            }

            await Shell.Current.GoToAsync(
                nameof(ArkNovaPreparationPage),
                new ShellNavigationQueryParameters
                {
                    [NavigationParameters.GroupId] = groupId
                });
        }
        catch
        {
            _viewModel.EndNavigation();
            throw;
        }
    }

    private async void OnEditGroupClicked(object? sender, EventArgs e)
    {
        if (sender is not ImageButton { CommandParameter: Guid groupId }
            || !_viewModel.TryBeginNavigation())
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync(
                nameof(EditGroupPage),
                new ShellNavigationQueryParameters { [NavigationParameters.GroupId] = groupId });
        }
        catch
        {
            _viewModel.EndNavigation();
            throw;
        }
    }
}
