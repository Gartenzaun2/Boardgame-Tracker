namespace BoardgameTracker.App.Presentation.Controls;

internal readonly record struct HeaderBox(double X, double Y, double Width, double Height);
internal sealed record HeaderFrame(HeaderBox Back, HeaderBox Title, HeaderBox Actions,
    HeaderBox Context, HeaderBox Metadata, HeaderBox Divider, double Height);

/// <summary>Geometry shared by the native header layout and platform-independent anchor tests.</summary>
internal static class AppPageHeaderGeometry
{
    internal const double DividerHorizontalInset = 6;

    internal static double TitleLeft(bool subpage) => subpage ? 44 : 16;
    internal static double TitleWidth(double width, double actionsWidth, bool subpage) =>
        Math.Max(0, width - TitleLeft(subpage) - (actionsWidth > 0 ? 6 + actionsWidth + 8 : 16));

    internal static HeaderFrame Create(double width, double titleHeight, double actionsWidth,
        double actionsHeight, double contextHeight, double metadataHeight, double dividerHeight, bool subpage)
    {
        // Touch targets may extend four dp beyond the visual title row. They stop
        // before the non-interactive context row, which never becomes a tap target.
        double rowHeight = Math.Max(36, Math.Max(titleHeight, actionsHeight - 8));
        double center = 4 + rowHeight / 2;
        double next = 4 + rowHeight;
        HeaderBox context = new(16, next + 4, Math.Max(0, width - 32), contextHeight);
        if (contextHeight > 0) next = context.Y + contextHeight;
        HeaderBox metadata = new(16, next + 4, Math.Max(0, width - 32), metadataHeight);
        if (metadataHeight > 0) next = metadata.Y + metadataHeight;
        HeaderBox divider = new(DividerHorizontalInset, next + 4,
            Math.Max(0, width - 2 * DividerHorizontalInset), dividerHeight);
        return new(new(2, center - 22, 44, 44),
            new(TitleLeft(subpage), 4, TitleWidth(width, actionsWidth, subpage), rowHeight),
            new(width - 6 - actionsWidth, center - actionsHeight / 2, actionsWidth, actionsHeight),
            context, metadata, divider, divider.Y + dividerHeight);
    }
}
