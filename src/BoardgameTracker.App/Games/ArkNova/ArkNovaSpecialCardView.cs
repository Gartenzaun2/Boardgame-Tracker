using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Games.ArkNova;

/// <summary>Shared icon-first display for persisted card data, including repeated symbols.</summary>
public sealed class ArkNovaSpecialCardView : ContentView
{
    public static readonly BindableProperty CardProperty = BindableProperty.Create(nameof(Card), typeof(ArkNovaSpecialCardData),
        typeof(ArkNovaSpecialCardView), null, propertyChanged: (bindable, _, _) => ((ArkNovaSpecialCardView)bindable).Refresh());
    public ArkNovaSpecialCardData? Card
    {
        get => (ArkNovaSpecialCardData?)GetValue(CardProperty);
        set => SetValue(CardProperty, value);
    }
    private void Refresh()
    {
        if (Card is not ArkNovaSpecialCardData card)
        {
            Content = null;
            return;
        }
        HorizontalStackLayout icons = new() { Spacing = 3, VerticalOptions = LayoutOptions.Center };
        foreach (ArkNovaCardSymbol symbol in card.Symbols)
        {
            Image image = new() { Source = ArkNovaCardSymbolPresentation.Image(symbol), WidthRequest = 26, HeightRequest = 26, Aspect = Aspect.AspectFit };
            SemanticProperties.SetDescription(image, ArkNovaCardSymbolPresentation.Name(symbol));
            icons.Add(image);
        }
        Label name = new() { Text = card.Name, FontSize = 14, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };
        name.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
        Grid grid = new() { ColumnSpacing = card.Symbols.Count == 0 ? 0 : 10 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.Add(icons);
        grid.Add(name, 1);
        Content = grid;
    }
}
