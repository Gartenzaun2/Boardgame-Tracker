using Microsoft.Maui.Layouts;

namespace BoardgameTracker.App.Presentation.Controls;

/// <summary>Measures the existing header's visual row independently of its touch targets.</summary>
public sealed class AppPageHeaderLayout : Layout
{
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant),
        typeof(AppPageHeaderVariant), typeof(AppPageHeaderLayout), AppPageHeaderVariant.Subpage,
        propertyChanged: (view, _, _) => ((AppPageHeaderLayout)view).InvalidateMeasure());
    public AppPageHeaderVariant Variant
    {
        get => (AppPageHeaderVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
    protected override ILayoutManager CreateLayoutManager() => new HeaderLayoutManager(this);

    // XAML slots: back, title, actions, context, metadata, divider. All touch
    // controls are direct children so their complete bounds participate in hit testing.
    private sealed class HeaderLayoutManager(AppPageHeaderLayout owner) : LayoutManager(owner)
    {
        private HeaderFrame? _frame;
        private double _measuredWidth;
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            if (owner.Children.Count != 6) return Size.Zero;
            bool subpage = owner.Variant == AppPageHeaderVariant.Subpage;
            Size actions = owner.Children[2].Measure(widthConstraint, double.PositiveInfinity);
            Size title = owner.Children[1].Measure(
                AppPageHeaderGeometry.TitleWidth(widthConstraint, actions.Width, subpage), double.PositiveInfinity);
            Size context = MeasureVisible(owner.Children[3], Math.Max(0, widthConstraint - 32));
            Size metadata = MeasureVisible(owner.Children[4], Math.Max(0, widthConstraint - 32));
            Size divider = owner.Children[5].Measure(
                Math.Max(0, widthConstraint - 2 * AppPageHeaderGeometry.DividerHorizontalInset), double.PositiveInfinity);
            owner.Children[0].Measure(44, 44);
            _measuredWidth = double.IsFinite(widthConstraint) ? widthConstraint : Math.Max(
                AppPageHeaderGeometry.TitleLeft(subpage) + title.Width + actions.Width + 24,
                Math.Max(context.Width, metadata.Width) + 32);
            _frame = AppPageHeaderGeometry.Create(_measuredWidth, title.Height, actions.Width,
                actions.Height, context.Height, metadata.Height, divider.Height, subpage);
            return new(_measuredWidth, _frame.Height);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            if (owner.Children.Count != 6) return bounds.Size;
            if (_frame is null || Math.Abs(_measuredWidth - bounds.Width) > 0.1)
                Measure(bounds.Width, double.PositiveInfinity);
            HeaderFrame frame = _frame!;
            HeaderBox[] boxes = [frame.Back, frame.Title, frame.Actions, frame.Context, frame.Metadata, frame.Divider];
            for (int index = 0; index < boxes.Length; index++)
            {
                HeaderBox box = boxes[index];
                owner.Children[index].Arrange(new Rect(bounds.X + box.X, bounds.Y + box.Y, box.Width, box.Height));
            }
            return new(bounds.Width, frame.Height);
        }
        private static Size MeasureVisible(IView view, double width) => view.Visibility == Visibility.Collapsed
            ? Size.Zero : view.Measure(width, double.PositiveInfinity);
    }
}
