using Microsoft.Maui.Layouts;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;

/// <summary>Wraps whole measured sections at the result page's available width.</summary>
public sealed class ArkNovaStartingOrderLayout : Layout
{
    protected override ILayoutManager CreateLayoutManager() => new OrderLayoutManager(this);
    private sealed class OrderLayoutManager(ArkNovaStartingOrderLayout owner) : LayoutManager(owner)
    {
        private StartingOrderBox[] _boxes = [];
        private double _width, _height;
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var sizes = owner.Children.Select(child => child.Measure(widthConstraint, double.PositiveInfinity))
                .Select(size => new StartingOrderSize(size.Width, size.Height)).ToArray();
            _width = double.IsFinite(widthConstraint) ? widthConstraint : sizes.Sum(size => size.Width) + Math.Max(0, sizes.Length - 1) * ArkNovaStartingOrderGeometry.Gap;
            (_boxes, _height) = ArkNovaStartingOrderGeometry.Rows(_width, sizes);
            return new(_width, _height);
        }
        public override Size ArrangeChildren(Rect bounds)
        {
            if (_boxes.Length != owner.Children.Count || Math.Abs(_width - bounds.Width) > 0.1)
                Measure(bounds.Width, double.PositiveInfinity);
            for (int index = 0; index < _boxes.Length; index++)
            {
                StartingOrderBox box = _boxes[index];
                owner.Children[index].Arrange(new Rect(bounds.X + box.X, bounds.Y + box.Y, box.Width, box.Height));
            }
            return new(bounds.Width, _height);
        }
    }
}

/// <summary>Separately measures the prefix/arrow and name, keeping an arrow with its name.</summary>
public sealed class ArkNovaStartingOrderSection : Layout
{
    public static readonly BindableProperty HasPreviousProperty = BindableProperty.Create(nameof(HasPrevious),
        typeof(bool), typeof(ArkNovaStartingOrderSection), false,
        propertyChanged: (view, _, _) => ((ArkNovaStartingOrderSection)view).InvalidateMeasure());
    public bool HasPrevious
    {
        get => (bool)GetValue(HasPreviousProperty);
        set => SetValue(HasPreviousProperty, value);
    }
    protected override ILayoutManager CreateLayoutManager() => new SectionLayoutManager(this);
    private sealed class SectionLayoutManager(ArkNovaStartingOrderSection owner) : LayoutManager(owner)
    {
        private StartingOrderSectionFrame? _frame;
        private double _width;
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            if (owner.Children.Count != 2) return Size.Zero;
            Size leading = owner.Children[0].Measure(double.PositiveInfinity, double.PositiveInfinity);
            Size naturalName = owner.Children[1].Measure(double.PositiveInfinity, double.PositiveInfinity);
            _width = double.IsFinite(widthConstraint) ? widthConstraint : leading.Width + ArkNovaStartingOrderGeometry.Gap + naturalName.Width;
            var lead = new StartingOrderSize(leading.Width, leading.Height);
            bool captionAbove = ArkNovaStartingOrderGeometry.CaptionNeedsOwnLine(_width, lead,
                new(naturalName.Width, naturalName.Height), owner.HasPrevious);
            double nameWidth = ArkNovaStartingOrderGeometry.NameWidth(_width, lead, captionAbove);
            Size name = naturalName.Width <= nameWidth ? naturalName
                : owner.Children[1].Measure(nameWidth, double.PositiveInfinity);
            _frame = ArkNovaStartingOrderGeometry.Section(_width, lead, new(name.Width, name.Height), captionAbove);
            return new(_frame.Size.Width, _frame.Size.Height);
        }
        public override Size ArrangeChildren(Rect bounds)
        {
            if (owner.Children.Count != 2) return bounds.Size;
            // The natural section width may be smaller than the available line width.
            // Reuse its measured frame, avoiding star-column expansion or extra indents.
            if (_frame is null) Measure(bounds.Width, double.PositiveInfinity);
            var frame = _frame!;
            var lead = frame.Leading;
            var name = frame.Name;
            owner.Children[0].Arrange(new Rect(bounds.X + lead.X, bounds.Y + lead.Y, lead.Width, lead.Height));
            owner.Children[1].Arrange(new Rect(bounds.X + name.X, bounds.Y + name.Y, name.Width, name.Height));
            return new(frame.Size.Width, frame.Size.Height);
        }
    }
}
