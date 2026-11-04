namespace BoardgameTracker.App.Presentation.Controls;

public enum AppPageHeaderVariant { MainSection, Subpage }

/// <summary>Shared header geometry and presentation slots; pages retain navigation ownership.</summary>
public partial class AppPageHeader : ContentView
{
    public AppPageHeader()
    {
        InitializeComponent();
        ActionItems.ChildAdded += (_, args) =>
        {
            if (args.Element is ImageButton image)
            {
                // Old pages may still supply 48-dp local requests. Header spacing
                // is owned here, independent of page padding and action count.
                image.WidthRequest = 44;
                image.HeightRequest = 44;
                image.VerticalOptions = LayoutOptions.Center;
            }
        };
    }

    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(AppPageHeaderVariant), typeof(AppPageHeader), AppPageHeaderVariant.Subpage, propertyChanged: HeaderChanged);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(AppPageHeader), string.Empty);
    public static readonly BindableProperty ShowBackProperty = BindableProperty.Create(nameof(ShowBack), typeof(bool), typeof(AppPageHeader), true, propertyChanged: HeaderChanged);
    public static readonly BindableProperty BadgeTextProperty = BindableProperty.Create(nameof(BadgeText), typeof(string), typeof(AppPageHeader), string.Empty, propertyChanged: HeaderChanged);
    public static readonly BindableProperty GameNameProperty = BindableProperty.Create(nameof(GameName), typeof(string), typeof(AppPageHeader), string.Empty);
    public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(AppPageHeader), string.Empty, propertyChanged: HeaderChanged);
    public static readonly BindableProperty MetadataProperty = BindableProperty.Create(nameof(Metadata), typeof(string), typeof(AppPageHeader), string.Empty, propertyChanged: HeaderChanged);

    public AppPageHeaderVariant Variant { get => (AppPageHeaderVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public bool ShowBack { get => (bool)GetValue(ShowBackProperty); set => SetValue(ShowBackProperty, value); }
    public string BadgeText { get => (string)GetValue(BadgeTextProperty); set => SetValue(BadgeTextProperty, value); }
    public string GameName { get => (string)GetValue(GameNameProperty); set => SetValue(GameNameProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string Metadata { get => (string)GetValue(MetadataProperty); set => SetValue(MetadataProperty, value); }
    public IList<IView> Actions => ActionItems.Children;
    public bool IsBackVisible => Variant == AppPageHeaderVariant.Subpage && ShowBack;
    public bool HasBadge => !string.IsNullOrWhiteSpace(BadgeText);
    public bool HasMetadata => !string.IsNullOrWhiteSpace(Metadata);
    // Metadata is measured separately; an absent context row consumes no height.
    public bool HasContextRow => HasBadge || !string.IsNullOrWhiteSpace(Subtitle);
    public event EventHandler? BackClicked;
    private void OnBackClicked(object? sender, EventArgs e) => BackClicked?.Invoke(this, e);

    private static void HeaderChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var header = (AppPageHeader)bindable;
        // Visibility changes never alter the subpage title anchor.
        header.OnPropertyChanged(nameof(IsBackVisible));
        header.OnPropertyChanged(nameof(HasBadge));
        header.OnPropertyChanged(nameof(HasMetadata));
        header.OnPropertyChanged(nameof(HasContextRow));
    }
}
