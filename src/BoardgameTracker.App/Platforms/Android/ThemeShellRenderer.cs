using Android.Content;
using Android.Graphics.Drawables;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
namespace BoardgameTracker.App.Platforms.Android;

public sealed class ThemeShellRenderer(Context context) : ShellRenderer(context)
{
    protected override IShellItemRenderer CreateShellItemRenderer(ShellItem shellItem)
        => new MainTabRenderer(this);

    private sealed class MainTabRenderer(IShellContext context) : ShellItemRenderer(context)
    {
        protected override bool OnItemSelected(global::Android.Views.IMenuItem item)
        {
            var tabs = ((IShellItemController)ShellItem).GetItems();
            if (context.Shell is AppShell shell && item.ItemId >= 0 && item.ItemId < tabs.Count)
            {
                ShellSection tab = tabs[item.ItemId];
                shell.Dispatcher.Dispatch(async () => await shell.NavigateFromMainTabTapAsync(tab));
                return false;
            }
            return base.OnItemSelected(item);
        }
    }

    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
        => new ThemeBottomBarTracker(this, shellItem);

    private sealed class ThemeBottomBarTracker(IShellContext context, ShellItem item)
        : ShellBottomNavViewAppearanceTracker(context, item)
    {
        public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
        {
            base.SetAppearance(bottomView, appearance);
            var resources = Microsoft.Maui.Controls.Application.Current!.Resources;
            Color Token(string key) => (Color)resources[key];
            using var background = new GradientDrawable();
            background.SetColor(Token("NavigationBackground").ToPlatform());
            background.SetStroke((int)Math.Ceiling(bottomView.Resources!.DisplayMetrics!.Density), Token("Outline").ToPlatform());
            bottomView.Background = background;
            // Material's built-in active indicator covers only the icon. The item
            // background includes the complete native item, including its label.
            bottomView.ItemActiveIndicatorEnabled = false;
            // Fixed icon-over-label layout, including the inactive tabs. Avoid the
            // shifting mode and oversized text metrics used by platform defaults.
            bottomView.LabelVisibilityMode = 1;
            bottomView.ItemHorizontalTranslationEnabled = false;
            float density = bottomView.Resources.DisplayMetrics.Density;
            bottomView.ItemIconSize = (int)(20 * density);
            bottomView.ItemPaddingTop = (int)(4 * density);
            bottomView.ItemPaddingBottom = (int)(4 * density);
            // Set both appearances through Material's API so it recalculates its
            // label scale/shift factors; changing TextViews directly leaves stale metrics.
            bottomView.ItemTextAppearanceInactive = global::BoardgameTracker.App.Resource.Style.BoardgameTrackerTabLabel;
            bottomView.ItemTextAppearanceActive = global::BoardgameTracker.App.Resource.Style.BoardgameTrackerTabLabel;
            using var selected = new GradientDrawable();
            selected.SetColor(Token("NavigationSelectedSurface").ToPlatform());
            selected.SetCornerRadius(12 * density);
            using var inset = new InsetDrawable(selected, (int)(6 * density), (int)(6 * density),
                (int)(6 * density), (int)(6 * density));
            using var states = new StateListDrawable();
            states.AddState([global::Android.Resource.Attribute.StateChecked], inset);
            states.AddState([], new ColorDrawable(global::Android.Graphics.Color.Transparent));
            bottomView.ItemBackground = states;

        }
    }
}
