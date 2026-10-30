using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Presentation.Controls;

/// <summary>App-styled modal messages, confirmations and single-choice lists.</summary>
public sealed class AppDialogPage : ContentPage
{
    private static readonly HashSet<Page> Owners = [];
    private readonly TaskCompletionSource<int> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _closing;

    private AppDialogPage(string title, string? message, string closeText,
        string? acceptText, bool destructive, IReadOnlyList<string>? options, int selectedIndex)
    {
        Shell.SetNavBarIsVisible(this, false);
        SetDynamicResource(BackgroundColorProperty, "BusyOverlay");
        Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetModalPresentationStyle(
            this.On<Microsoft.Maui.Controls.PlatformConfiguration.iOS>(),
            Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.UIModalPresentationStyle.OverFullScreen);

        Label heading = new() { Text = title, FontSize = 20, FontAttributes = FontAttributes.Bold };
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level1);
        VerticalStackLayout body = new() { Spacing = 8 };
        if (message is not null)
            body.Children.Add(new Label { Text = message });

        if (options is not null)
        {
            for (int index = 0; index < options.Count; index++)
            {
                int choice = index;
                bool selected = index == selectedIndex;
                Button option = new()
                {
                    Text = selected ? $"✓  {options[index]}" : options[index],
                    Style = Resource<Style>("SecondaryActionButton"),
                    HorizontalOptions = LayoutOptions.Fill,
                    MinimumHeightRequest = 48,
                    AutomationId = $"DialogOption{index}"
                };
                option.SetDynamicResource(Button.BackgroundColorProperty, selected ? "SelectedSurface" : "Surface");
                option.SetDynamicResource(Button.BorderColorProperty, selected ? "SelectedBorder" : "Outline");
                SemanticProperties.SetDescription(option, selected
                    ? $"{options[index]} · {AppResources.SelectedSymbol}"
                    : options[index]);
                option.Clicked += async (_, _) => await CloseAsync(choice);
                body.Children.Add(option);
            }
        }

        VerticalStackLayout actions = new() { Spacing = 8 };
        if (acceptText is not null)
        {
            Button accept = new()
            {
                Text = acceptText,
                Style = Resource<Style>(destructive ? "DangerButton" : "PrimaryCompactButton"),
                AutomationId = "DialogConfirm"
            };
            accept.Clicked += async (_, _) => await CloseAsync(0);
            actions.Children.Add(accept);
        }

        Button close = new()
        {
            Text = closeText,
            Style = Resource<Style>("TextActionButton"),
            AutomationId = "DialogCancel"
        };
        close.Clicked += async (_, _) => await CloseAsync(-1);
        actions.Children.Add(close);

        Grid layout = new()
        {
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)],
            RowSpacing = 12
        };
        layout.Add(heading, 0, 0);
        ScrollView scroll = new() { Content = body };
        layout.Add(scroll, 0, 1);
        layout.Add(actions, 0, 2);
        Border dialog = new() { Style = Resource<Style>("SelectionDialog"), Content = layout };
        Grid backdrop = new() { Padding = 18, Children = { dialog } };
        void ResizeDialog()
        {
            double available = Math.Max(0, backdrop.Height - 36);
            dialog.MaximumHeightRequest = available;
            scroll.MaximumHeightRequest = Math.Max(0, available - heading.Height - actions.Height - 56);
        }
        backdrop.SizeChanged += (_, _) => ResizeDialog();
        heading.SizeChanged += (_, _) => ResizeDialog();
        actions.SizeChanged += (_, _) => ResizeDialog();
        Content = backdrop;
    }

    // Modal dismissal must not reload an owner's unsaved form or correction draft.
    public static bool IsOpen(Page owner) => Owners.Contains(owner);

    public static async Task<bool> ShowAsync(Page owner, string title, string message,
        string accept, string? cancel = null, bool destructive = false)
    {
        AppDialogPage dialog = new(title, message, cancel ?? accept,
            cancel is null ? null : accept, destructive, null, -1);
        return await PresentAsync(owner, dialog) == 0;
    }

    public static Task<int> ChooseAsync(Page owner, string title, IReadOnlyList<string> options, int selectedIndex, string? cancelText = null) =>
        PresentAsync(owner, new AppDialogPage(title, null, cancelText ?? AppResources.Close, null, false, options, selectedIndex));

    private static async Task<int> PresentAsync(Page owner, AppDialogPage dialog)
    {
        if (!Owners.Add(owner))
            return -1;

        try
        {
            await owner.Navigation.PushModalAsync(dialog, animated: false);
            return await dialog._completion.Task;
        }
        finally
        {
            Owners.Remove(owner);
        }
    }

    private async Task CloseAsync(int result)
    {
        if (_closing)
            return;

        _closing = true;
        try
        {
            await Navigation.PopModalAsync(animated: false);
            _completion.TrySetResult(result);
        }
        catch (Exception exception)
        {
            _completion.TrySetException(exception);
        }
    }

    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () => await CloseAsync(-1));
        return true;
    }

    private static T Resource<T>(string key) => (T)Microsoft.Maui.Controls.Application.Current!.Resources[key];
}
