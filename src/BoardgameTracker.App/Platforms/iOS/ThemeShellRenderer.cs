using CoreGraphics;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
using UIKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BoardgameTracker.App.Resources.Localization;
namespace BoardgameTracker.App.Platforms.iOS;

public sealed class ThemeShellRenderer : ShellRenderer
{
    protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker() => new ThemeTabBarTracker();

    private sealed class ThemeTabBarTracker : ShellTabBarAppearanceTracker
    {
        private static readonly ConditionalWeakTable<UITabBar, UITapGestureRecognizer> TabTaps = new();

        private static void ConnectTabTaps(UITabBarController controller)
        {
            UITabBar bar = controller.TabBar;
            TabTaps.GetValue(bar, _ =>
            {
                UITapGestureRecognizer tap = new(recognizer =>
                {
                    if (Shell.Current is not AppShell shell)
                        return;
                    // Use actual native item frames, including safe-area/RTL layout.
                    UIView? item = bar.Subviews.OfType<UIControl>()
                        .FirstOrDefault(view => view.Frame.Contains(recognizer.LocationInView(bar)));
                    if (item is null || controller.ViewControllers is null)
                        return;
                    UIControl[] controls = bar.Subviews.OfType<UIControl>().OrderBy(view => view.Frame.X).ToArray();
                    int index = Array.IndexOf(controls, item);
                    if (bar.EffectiveUserInterfaceLayoutDirection == UIUserInterfaceLayoutDirection.RightToLeft)
                        index = controls.Length - 1 - index;
                    if (index < 0 || index >= shell.CurrentItem.Items.Count)
                        return;
                    ShellSection target = shell.CurrentItem.Items[index];
                    ILogger<ThemeShellRenderer> logger = shell.Handler!.MauiContext!.Services.GetRequiredService<ILogger<ThemeShellRenderer>>();
                    shell.Dispatcher.Dispatch(async () =>
                    {
                        try
                        {
                            await shell.NavigateFromMainTabTapAsync(target);
                        }
                        catch (Exception exception)
                        {
                            logger.LogError(exception, "Could not navigate from an iOS main tab tap.");
                            // Keep native event exceptions out of the iOS run loop.
                            try
                            {
                                await shell.DisplayAlertAsync(AppResources.NavigationErrorTitle, AppResources.NavigationFailed, AppResources.Ok);
                            }
                            catch (Exception dialogException)
                            {
                                logger.LogError(dialogException, "Could not display the main tab navigation error.");
                            }
                        }
                    });
                }) { CancelsTouchesInView = true, DelaysTouchesEnded = true };
                // Shell owns selection and root reset; do not replace any native controller/delegate.
                bar.AddGestureRecognizer(tap);
                return tap;
            });
        }

        public override void SetAppearance(UITabBarController controller, ShellAppearance appearance)
        {
            base.SetAppearance(controller, appearance);
            Apply(controller);
        }
        public override void UpdateLayout(UITabBarController controller)
        {
            base.UpdateLayout(controller);
            Apply(controller);
        }
        private static void Apply(UITabBarController controller)
        {
            ConnectTabTaps(controller);
            var bar = controller.TabBar;
            bar.Translucent = false;
            bar.ItemPositioning = UITabBarItemPositioning.Fill;
            var resources = Microsoft.Maui.Controls.Application.Current!.Resources;
            UIColor Token(string key) => ((Color)resources[key]).ToPlatform();
            var appearance = bar.StandardAppearance;
            appearance.BackgroundColor = Token("NavigationBackground");
            appearance.ShadowColor = Token("Outline");
            bar.StandardAppearance = appearance;
            bar.ScrollEdgeAppearance = appearance;
            var width = bar.Bounds.Width / Math.Max(1, bar.Items?.Length ?? 1);
            if (width <= 16) return;
            using var renderer = new UIGraphicsImageRenderer(new CGSize(width, 49));
            using var indicator = renderer.CreateImage(_ =>
            {
                Token("NavigationSelectedSurface").SetFill();
                using var path = UIBezierPath.FromRoundedRect(new CGRect(6, 2, width - 12, 44), 12);
                path.Fill();
            });
            bar.SelectionIndicatorImage = indicator.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
        }
    }
}
