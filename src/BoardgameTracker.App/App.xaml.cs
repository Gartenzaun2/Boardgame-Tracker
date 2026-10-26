using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Infrastructure.Persistence.SQLite;

namespace BoardgameTracker.App;

public partial class App : Microsoft.Maui.Controls.Application {
    private readonly AppShell _shell;
    private readonly DatabaseInitializer _databaseInitializer;

    public App(
        AppShell shell,
        DatabaseInitializer databaseInitializer)
    {
        InitializeComponent();

        _shell = shell;
        _databaseInitializer = databaseInitializer;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        ContentPage startupPage = CreateStartupPage();

        Window window = new(startupPage);

        window.Created += async (_, _) =>
        {
            try
            {
                await _databaseInitializer.InitializeAsync();

                window.Page = _shell;
            }
            catch (Exception exception)
            {
                window.Page = CreateStartupErrorPage(exception);
            }
        };

        return window;
    }

    private static ContentPage CreateStartupPage()
    {
        return new ContentPage
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
    }

    private static ContentPage CreateStartupErrorPage(Exception exception)
    {
        return new ContentPage
        {
            Padding = 24,
            Content = new VerticalStackLayout
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
                    new Label
                    {
                        Text = AppResources.StartupErrorMessage
                    },
#if DEBUG
                    new Label
                    {
                        Text = exception.ToString(),
                        FontSize = 11
                    }
#endif
                }
            }
        };
    }
}
