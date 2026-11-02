using System.Reflection;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class ArkNovaScoringNavigationTests
{
    [Theory]
    [InlineData(typeof(ArkNovaScoringPage))]
    [InlineData(typeof(ArkNovaRunningPage))]
    [InlineData(typeof(ArkNovaResultPage))]
    public void ReturnWithoutSessionParameterRetainsExistingContext(Type pageType)
    {
        object page = Activator.CreateInstance(pageType)!;
        Guid sessionId = Guid.NewGuid();
        Apply(page, new() { [NavigationParameters.GameSessionId] = sessionId,
            [NavigationParameters.ResultIsHistorical] = true });

        // A Shell pop can carry other query entries after single-use parameters were consumed.
        Apply(page, new() { ["unrelated"] = "return" });
        Assert.Equal(sessionId, SessionId(page));
        Apply(page, new());
        Assert.Equal(sessionId, SessionId(page));
        if (page is ArkNovaResultPage result)
        {
            Assert.True(result.BackButton.IsVisible);
            Assert.True(result.EditButton.IsVisible);
            Assert.False(result.DoneButton.IsVisible);
        }
    }

    [Theory]
    [InlineData(typeof(ArkNovaScoringPage))]
    [InlineData(typeof(ArkNovaRunningPage))]
    [InlineData(typeof(ArkNovaResultPage))]
    public void FirstVisitRequiresSessionIdEvenWithUnrelatedParameters(Type pageType)
    {
        object page = Activator.CreateInstance(pageType)!;
        Assert.Throws<InvalidOperationException>(() => Apply(page, new()));
        Assert.Throws<InvalidOperationException>(() => Apply(page, new() { ["unrelated"] = true }));
    }

    [Theory]
    [InlineData(typeof(ArkNovaScoringPage), false)]
    [InlineData(typeof(ArkNovaScoringPage), true)]
    [InlineData(typeof(ArkNovaRunningPage), false)]
    [InlineData(typeof(ArkNovaRunningPage), true)]
    [InlineData(typeof(ArkNovaResultPage), false)]
    [InlineData(typeof(ArkNovaResultPage), true)]
    public void ExplicitInvalidSessionIsRejectedWithoutReplacingContext(Type pageType, bool initialized)
    {
        object page = Activator.CreateInstance(pageType)!;
        Guid sessionId = initialized ? Guid.NewGuid() : Guid.Empty;
        if (initialized)
            Apply(page, new() { [NavigationParameters.GameSessionId] = sessionId });

        foreach (object invalid in new object[] { Guid.Empty, "invalid", Guid.NewGuid().ToString(), null! })
        {
            Assert.Throws<InvalidOperationException>(() => Apply(page,
                new() { [NavigationParameters.GameSessionId] = invalid }));
            Assert.Equal(sessionId, SessionId(page));
        }
    }

    [Fact]
    public void ExplicitValidSessionCanReplacePreviousContext()
    {
        ArkNovaScoringPage page = new();
        Apply(page, new() { [NavigationParameters.GameSessionId] = Guid.NewGuid() });
        Guid next = Guid.NewGuid();
        Apply(page, new() { [NavigationParameters.GameSessionId] = next });
        Assert.Equal(next, SessionId(page));
    }

    private static Guid SessionId(object page) =>
        (Guid)page.GetType().GetField("_gameSessionId", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page)!;

    private static void Apply(object page, Dictionary<string, object> query)
    {
        switch (page)
        {
            case ArkNovaScoringPage scoring: scoring.ApplyQueryAttributes(query); break;
            case ArkNovaRunningPage running: running.ApplyQueryAttributes(query); break;
            case ArkNovaResultPage result: result.ApplyQueryAttributes(query); break;
            default: throw new ArgumentException("Unsupported page.", nameof(page));
        }
    }
}
