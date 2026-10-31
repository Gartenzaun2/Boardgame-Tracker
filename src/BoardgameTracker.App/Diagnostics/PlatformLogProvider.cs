using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Diagnostics;

/// <summary>Retains technical failure diagnostics in release device logs, never in user-facing error text.</summary>
public sealed class PlatformLogProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new PlatformLogger(categoryName);
    public void Dispose() { }
    private sealed class PlatformLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Warning;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(level)) return;
            string text = $"{category}: {formatter(state, exception)}\n{exception}";
#if ANDROID
            Android.Util.Log.Error("BoardgameTracker", text);
#else
            Console.Error.WriteLine(text);
#endif
        }
    }
}
