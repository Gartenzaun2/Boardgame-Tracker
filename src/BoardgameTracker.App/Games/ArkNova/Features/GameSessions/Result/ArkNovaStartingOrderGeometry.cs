namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;

internal readonly record struct StartingOrderSize(double Width, double Height);
internal readonly record struct StartingOrderBox(double X, double Y, double Width, double Height);
internal sealed record StartingOrderSectionFrame(StartingOrderBox Leading, StartingOrderBox Name, StartingOrderSize Size);

/// <summary>Packs measured sections; no character counts or historical ordering decisions.</summary>
internal static class ArkNovaStartingOrderGeometry
{
    internal const double Gap = 5;
    internal const double RowGap = 4;

    internal static bool CaptionNeedsOwnLine(double width, StartingOrderSize leading, StartingOrderSize naturalName, bool hasPrevious) =>
        !hasPrevious && naturalName.Width <= width && leading.Width + Gap + naturalName.Width > width;

    internal static double NameWidth(double width, StartingOrderSize leading, bool captionAbove) =>
        Math.Max(1, captionAbove ? width : width - leading.Width - Gap);

    internal static StartingOrderSectionFrame Section(double width, StartingOrderSize leading, StartingOrderSize name, bool captionAbove)
    {
        if (captionAbove)
            return new(new(0, 0, leading.Width, leading.Height), new(0, leading.Height + RowGap, name.Width, name.Height),
                new(width, leading.Height + RowGap + name.Height));
        double height = Math.Max(leading.Height, name.Height);
        return new(new(0, (height - leading.Height) / 2, leading.Width, leading.Height),
            new(leading.Width + Gap, (height - name.Height) / 2, name.Width, name.Height),
            new(leading.Width + Gap + name.Width, height));
    }

    internal static (StartingOrderBox[] Boxes, double Height) Rows(double width, IReadOnlyList<StartingOrderSize> sections)
    {
        StartingOrderBox[] boxes = new StartingOrderBox[sections.Count];
        double x = 0, y = 0, rowHeight = 0;
        int rowStart = 0;
        void CenterRow(int end)
        {
            for (int index = rowStart; index < end; index++)
                boxes[index] = boxes[index] with { Y = y + (rowHeight - boxes[index].Height) / 2 };
        }
        for (int index = 0; index < sections.Count; index++)
        {
            StartingOrderSize section = sections[index];
            double gap = index > rowStart ? Gap : 0;
            if (index > rowStart && x + gap + section.Width > width)
            {
                CenterRow(index);
                y += rowHeight + RowGap;
                rowStart = index;
                x = 0;
                rowHeight = 0;
                gap = 0;
            }
            boxes[index] = new(x + gap, y, section.Width, section.Height);
            x += gap + section.Width;
            rowHeight = Math.Max(rowHeight, section.Height);
        }
        CenterRow(sections.Count);
        return (boxes, sections.Count == 0 ? 0 : y + rowHeight);
    }
}
