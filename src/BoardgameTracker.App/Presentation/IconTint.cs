using Microsoft.Maui.Platform;
namespace BoardgameTracker.App.Presentation;

// Tint bundled monochrome UI icons without changing game artwork or adding a toolkit.
public static class IconTint
{
    public static readonly BindableProperty ColorProperty = BindableProperty.CreateAttached(
        "Color", typeof(Color), typeof(IconTint), null, propertyChanged: OnColorChanged);
    public static Color? GetColor(BindableObject view) => (Color?)view.GetValue(ColorProperty);
    public static void SetColor(BindableObject view, Color? color) => view.SetValue(ColorProperty, color);
    private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not View view) return;
        view.HandlerChanged -= OnViewReady;
        view.Loaded -= OnViewReady;
        view.HandlerChanged += OnViewReady;
        view.Loaded += OnViewReady;
        Apply(view);
    }
    private static void OnViewReady(object? sender, EventArgs e)
    {
        if (sender is View view) Apply(view);
    }
    private static void Apply(View view)
    {
        if (GetColor(view) is not Color color) return;
#if ANDROID
        if (view.Handler?.PlatformView is global::Android.Widget.ImageView image)
            image.ImageTintList = global::Android.Content.Res.ColorStateList.ValueOf(color.ToPlatform());
#elif IOS
        if (view.Handler?.PlatformView is UIKit.UIButton button)
        {
            button.TintColor = color.ToPlatform();
            if (button.CurrentImage is { } source)
            {
                using var template = source.ImageWithRenderingMode(UIKit.UIImageRenderingMode.AlwaysTemplate);
                button.SetImage(template, UIKit.UIControlState.Normal);
            }
        }
        else if (view.Handler?.PlatformView is UIKit.UIImageView image)
        {
            image.TintColor = color.ToPlatform();
            if (image.Image is { } source)
                image.Image = source.ImageWithRenderingMode(UIKit.UIImageRenderingMode.AlwaysTemplate);
        }
#endif
    }
}
