using BoardgameTracker.App.Features.GameSessions.Running;
using BoardgameTracker.App.Features.GameSessions.Scoring;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.GameSessions.Resume;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App;

/// <summary>
/// MAUI application bootstrap. It initializes the database before showing the Shell
/// and restores the last explicit navigation context when possible.
/// </summary>
public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IServiceProvider _services;
    private readonly DatabaseInitializer _databaseInitializer;
    private readonly ILogger<App> _logger;

    public App(
        IServiceProvider services,
        DatabaseInitializer databaseInitializer,
        ILogger<App> logger)
    {
        InitializeComponent();
        _services = services;
        _databaseInitializer = databaseInitializer;
        _logger = logger;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = new(CreateStartupPage());
        window.Created += async (_, _) => await InitializeApplicationAsync(window);
        return window;
    }

    private async Task InitializeApplicationAsync(Window window)
    {
        window.Page = CreateStartupPage();

        try
        {
            await _databaseInitializer.InitializeAsync();
            AppShell shell = _services.GetRequiredService<AppShell>();
            window.Page = shell;

            IAppNavigationSettingsStore navigationStore =
                _services.GetRequiredService<IAppNavigationSettingsStore>();
            IOpenGameSessionQuery openSessionQuery =
                _services.GetRequiredService<IOpenGameSessionQuery>();

            AppNavigationSettings state = navigationStore.Get();

            if (state.LastSessionId is Guid rememberedSessionId)
            {
                OpenGameSession? rememberedSession =
                    await openSessionQuery.GetAsync(rememberedSessionId);

                if (rememberedSession is not null && rememberedSession.GameKey == ArkNovaGame.Key)
                {
                    await RestoreSessionAsync(shell, rememberedSession);
                    shell.EnableNavigationMemory();
                    return;
                }

                state = state with { LastSessionId = null };
                navigationStore.Save(state);
            }

            // Compatibility for an existing development database created before
            // explicit navigation context existed: restore only when there is
            // exactly one unfinished session. Never choose arbitrarily among several.
            if (state.LastMainSection is null)
            {
                IReadOnlyList<OpenGameSession> openSessions = await openSessionQuery.GetAllAsync();
                if (openSessions.Count == 1 && openSessions[0].GameKey == ArkNovaGame.Key)
                {
                    await RestoreSessionAsync(shell, openSessions[0]);
                    shell.EnableNavigationMemory();
                    return;
                }
            }

            AppMainSection initialSection = ResolveInitialSection(state);
            if (initialSection == AppMainSection.Groups)
            {
                string? gameKey = state.SelectedGameKey ?? state.DefaultGameKey;
                if (!string.IsNullOrWhiteSpace(gameKey))
                {
                    await shell.NavigateToGroupsForGameAsync(gameKey, remember: false);
                }
                else
                {
                    await shell.NavigateToMainSectionAsync(AppMainSection.Games, remember: false);
                }
            }
            else
            {
                await shell.NavigateToMainSectionAsync(initialSection, remember: false);
            }

            shell.EnableNavigationMemory();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "The application database could not be initialized.");
            window.Page = CreateStartupErrorPage(window, exception);
        }
    }

    private static async Task RestoreSessionAsync(
        AppShell shell,
        OpenGameSession session)
    {
        await shell.NavigateToGroupsForGameAsync(session.GameKey, remember: false);
        shell.RememberSession(session.SessionId, session.GameKey);
        await NavigateToSessionAsync(shell, session);
    }

    private static AppMainSection ResolveInitialSection(AppNavigationSettings state)
    {
        if (state.LastMainSection is AppMainSection rememberedSection)
        {
            if (rememberedSection != AppMainSection.Groups
                || !string.IsNullOrWhiteSpace(state.SelectedGameKey)
                || !string.IsNullOrWhiteSpace(state.DefaultGameKey))
            {
                return rememberedSection;
            }
        }

        return !string.IsNullOrWhiteSpace(state.DefaultGameKey)
            ? AppMainSection.Groups
            : AppMainSection.Games;
    }

    private static async Task NavigateToSessionAsync(AppShell shell, OpenGameSession openSession)
    {
        string route = openSession.Status switch
        {
            GameSessionStatus.Running => nameof(ArkNovaRunningPage),
            GameSessionStatus.Scoring => nameof(ArkNovaScoringPage),
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(route))
        {
            return;
        }

        await shell.GoToAsync(
            route,
            new ShellNavigationQueryParameters
            {
                [NavigationParameters.SessionId] = openSession.SessionId
            });
    }

    private static ContentPage CreateStartupPage() =>
        new()
        {
            Content = new Grid
            {
                Children =
                {
                    new VerticalStackLayout
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        Spacing = 16,
                        Children =
                        {
                            new ActivityIndicator
                            {
                                IsRunning = true,
                                WidthRequest = 32,
                                HeightRequest = 32
                            },
                            new Label
                            {
                                Text = AppResources.Initializing,
                                HorizontalTextAlignment = TextAlignment.Center
                            }
                        }
                    }
                }
            }
        };

    private ContentPage CreateStartupErrorPage(Window window, Exception exception)
    {
        VerticalStackLayout content = new()
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = AppResources.StartupErrorTitle,
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold
                },
                new Label { Text = AppResources.StartupErrorMessage }
            }
        };

        Button retryButton = new()
        {
            Text = AppResources.Retry,
            HorizontalOptions = LayoutOptions.Start
        };
        retryButton.Clicked += async (_, _) =>
        {
            retryButton.IsEnabled = false;
            await InitializeApplicationAsync(window);
        };
        content.Children.Add(retryButton);

#if DEBUG
        content.Children.Add(new Label
        {
            Text = AppResources.TechnicalDetails,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 12, 0, 0)
        });
        content.Children.Add(new ScrollView
        {
            MaximumHeightRequest = 260,
            Content = new Label
            {
                Text = exception.ToString(),
                FontSize = 11
            }
        });
#endif

        return new ContentPage
        {
            Padding = 24,
            Content = content
        };
    }
}
