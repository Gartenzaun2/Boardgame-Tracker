using CoreGraphics;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
using UIKit;
namespace BoardgameTracker.App.Platforms.iOS;

public sealed class ThemeShellRenderer : ShellRenderer
{
    protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker() => new ThemeTabBarTracker();

    private sealed class ThemeTabBarTracker : ShellTabBarAppearanceTracker
    {
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
