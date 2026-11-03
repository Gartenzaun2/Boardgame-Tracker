// A platform-free preference double lets the tests exercise the production adapter.
namespace Microsoft.Maui.Storage
{
    public interface IPreferences
    {
        bool ContainsKey(string key, string? sharedName = null);
        void Remove(string key, string? sharedName = null);
        void Clear(string? sharedName = null);
        void Set<T>(string key, T value, string? sharedName = null);
        T Get<T>(string key, T defaultValue, string? sharedName = null);
    }
}

namespace BoardgameTracker.Application.Tests.Persistence
{
    internal sealed class TestPreferences : Microsoft.Maui.Storage.IPreferences
    {
        public Dictionary<string, object> Values { get; } = new(StringComparer.Ordinal);
        public bool FailWrites { get; set; }
        public bool ContainsKey(string key, string? sharedName = null) => Values.ContainsKey(key);
        public void Remove(string key, string? sharedName = null)
        {
            if (FailWrites)
                throw new IOException("Simulated preference failure.");
            Values.Remove(key);
        }
        public void Clear(string? sharedName = null) => Values.Clear();
        public void Set<T>(string key, T value, string? sharedName = null)
        {
            if (FailWrites)
                throw new IOException("Simulated preference failure.");
            Values[key] = value!;
        }
        public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
            Values.TryGetValue(key, out object? value) ? (T)value : defaultValue;
    }
}
