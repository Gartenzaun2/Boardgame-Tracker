using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.GameSessions;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Application.Persistence;
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
    private readonly IAppNavigationSettingsStore _navigationSettingsStore;
    private readonly OpenGameSessionQuery _openGameSessionQuery;
    private readonly DatabaseInitializer _databaseInitializer;
    private readonly ILogger<App> _logger;
    private readonly IEnumerable<IGameSessionResumeHandler> _resumeHandlers;

    public App(
        IServiceProvider services,
        DatabaseInitializer databaseInitializer,
        ILogger<App> logger,
        BoardgameTracker.App.Theming.ThemeManager themes,
        IAppNavigationSettingsStore settings,
        IEnumerable<IGameSessionResumeHandler> resumeHandlers,
        OpenGameSessionQuery openGameSessionQuery)
    {
        InitializeComponent();
        themes.Apply(settings.Get().Theme, persist: false);
        // AppShell is constructed after InitializeComponent and theme application.
        // Resolving it lazily preserves resource/lifecycle ordering for its pages.
        _services = services;
        _navigationSettingsStore = settings;
        _openGameSessionQuery = openGameSessionQuery;
        _databaseInitializer = databaseInitializer;
        _logger = logger;
        _resumeHandlers = resumeHandlers;
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

            AppNavigationSettings state = _navigationSettingsStore.Get();

            if (state.LastGameSessionId is Guid rememberedGameSessionId)
            {
                OpenGameSession? rememberedGameSession =
                    await _openGameSessionQuery.GetAsync(rememberedGameSessionId);

                IGameSessionResumeHandler? handler = rememberedGameSession is null
                    ? null
                    : _resumeHandlers.FirstOrDefault(candidate => candidate.CanHandle(rememberedGameSession.GameKey));
                if (rememberedGameSession is not null && handler is not null)
                {
                    await handler.ResumeAsync(shell, rememberedGameSession);
                    shell.EnableNavigationMemory();
                    return;
                }

                state = state with { LastGameSessionId = null };
                _navigationSettingsStore.Save(state);
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
