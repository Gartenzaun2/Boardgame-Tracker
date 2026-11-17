using System.Globalization;
using System.Xml.Linq;
using BoardgameTracker.App.Presentation.Converters;
using Microsoft.Maui.Controls;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class EnumEqualsConverterTests {
    public enum Choice { None, First, Second }
    public enum OtherChoice { None, First }

    [Theory]
    [InlineData(Choice.None, Choice.None, true)]
    [InlineData(Choice.First, Choice.First, true)]
    [InlineData(OtherChoice.First, OtherChoice.First, true)]
    [InlineData(Choice.First, Choice.Second, false)]
    [InlineData(Choice.First, OtherChoice.First, false)]
    [InlineData(Choice.First, "First", false)]
    [InlineData("First", Choice.First, false)]
    [InlineData(1, Choice.First, false)]
    [InlineData(Choice.First, 1, false)]
    [InlineData(null, Choice.First, false)]
    [InlineData(Choice.First, null, false)]
    [InlineData(null, null, false)]
    [InlineData((Choice)99, (Choice)99, false)]
    public void Convert_RequiresSameDefinedMemberAndEnumType(object? value, object? parameter, bool expected) {
        EnumEqualsConverter converter = new();
        Assert.Equal(expected, converter.Convert(value, typeof(bool), parameter, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ConvertBack_LeavesTheSourceUnchanged() {
        EnumEqualsConverter converter = new();
        Assert.Same(BindableProperty.UnsetValue, converter.ConvertBack(true, typeof(Choice), Choice.First, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AppResources_RegisterOneSharedEnumEqualsConverter() {
        using Stream stream = typeof(EnumEqualsConverterTests).Assembly.GetManifestResourceStream("ThemeContract.App.xaml")!;
        XDocument app = XDocument.Load(stream);
        XName key = XName.Get("Key", "http://schemas.microsoft.com/winfx/2009/xaml");
        XElement converter = Assert.Single(app.Descendants(), element => (string?)element.Attribute(key) == "EnumEquals");
        Assert.Equal("EnumEqualsConverter", converter.Name.LocalName);
        Assert.Equal("clr-namespace:BoardgameTracker.App.Presentation.Converters", converter.Name.NamespaceName);
    }
}
