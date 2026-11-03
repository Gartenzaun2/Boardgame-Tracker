// Preparation tests exercise the production view model without a platform UI.
namespace Microsoft.Maui.Graphics;

public sealed record Color(string Argb)
{
    public static Color FromArgb(string value) => new(value);
}
