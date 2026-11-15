using System.Globalization;
using System.Xml.Linq;
using BoardgameTracker.App;
using BoardgameTracker.App.Presentation.Converters;
using BoardgameTracker.Application.Tests.Persistence;

namespace BoardgameTracker.Application.Tests.Presentation {
    public sealed class SharedPresentationTests {
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void BoolNegation_InvertsBothBindingDirections(bool input, bool expected) {
            BoolNegationConverter converter = new();
            Assert.Equal(expected, converter.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture));
            Assert.Equal(expected, converter.ConvertBack(input, typeof(bool), null, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("true")]
        [InlineData(1)]
        public void BoolNegation_UnsupportedValuesLeaveTargetUnset(object? input) {
            BoolNegationConverter converter = new();
            Assert.Same(BindableProperty.UnsetValue, converter.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture));
            Assert.Same(BindableProperty.UnsetValue, converter.ConvertBack(input, typeof(bool), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task CurrentInstance_TracksActiveShellWithoutCaching() {
            await using SliceDatabase database = await SliceDatabase.CreateAsync();
            Shell previous = Shell.Current;
            try {
                AppShell first = MainSectionNavigationTests.CreateShell(new(), database);
                Shell.Current = first;
                Assert.Same(first, AppShell.CurrentInstance);

                AppShell second = MainSectionNavigationTests.CreateShell(new(), database);
                Shell.Current = second;
                Assert.Same(second, AppShell.CurrentInstance);
            }
            finally {
                Shell.Current = previous;
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CurrentInstance_RejectsMissingOrOtherShell(bool useOtherShell) {
            Shell previous = Shell.Current;
            try {
                Shell.Current = useOtherShell ? new Shell() : null!;
                InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => AppShell.CurrentInstance);
                Assert.Equal("The current Shell is not an AppShell.", error.Message);
            }
            finally {
                Shell.Current = previous;
            }
        }

        [Fact]
        public void SharedNegationResource_BindsOriginalObservableStates() {
            using Stream appStream = typeof(SharedPresentationTests).Assembly.GetManifestResourceStream("ThemeContract.App.xaml")!;
            XDocument app = XDocument.Load(appStream);
            Assert.Single(app.Descendants(), element => element.Name.LocalName == "BoolNegationConverter"
                && (string?)element.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2009/xaml")) == "NegateBool");

            XDocument games = AppPageHeaderTests.ReadPage("GamesPage");
            XElement createGame = Assert.Single(games.Descendants(), element => (string?)element.Attribute("Clicked") == "OnCreateGameClicked");
            Assert.Equal("{Binding IsBusy, Converter={StaticResource NegateBool}}", (string?)createGame.Attribute("IsEnabled"));

            using Stream groupStream = typeof(SharedPresentationTests).Assembly.GetManifestResourceStream("GeneralGroupFeature.EditGroupPage.xaml")!;
            XDocument group = XDocument.Load(groupStream);
            XElement archiveHint = Assert.Single(group.Descendants(), element => (string?)element.Attribute("Text") == "{x:Static res:AppResources.GroupArchiveLocked}");
            Assert.Equal("{Binding CanArchiveGroup, Converter={StaticResource NegateBool}}", (string?)archiveHint.Attribute("IsVisible"));
        }
    }
}

// Only MAUI's converter boundary is doubled; the linked converter is production code.
namespace Microsoft.Maui.Controls {
    public interface IValueConverter {
        object Convert(object? value, Type targetType, object? parameter, CultureInfo culture);
        object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
    }

    public static class BindableProperty {
        public static readonly object UnsetValue = new();
    }
}
