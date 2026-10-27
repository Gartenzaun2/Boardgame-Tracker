namespace BoardgameTracker.Core.Games.ArkNova;

/// <summary>
/// Represents stable Ark Nova zoo-plan reference data persisted by id, localized name key and image key.
/// </summary>
public sealed class ArkNovaZooPlan
{
    private ArkNovaZooPlan()
    {
    }

    public ArkNovaZooPlan(int id, int number, string nameResourceKey, string iconKey)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameResourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconKey);

        Id = id;
        Number = number;
        NameResourceKey = nameResourceKey;
        IconKey = iconKey;
    }

    public int Id { get; private set; }

    public int Number { get; private set; }

    public string NameResourceKey { get; private set; } = string.Empty;

    public string IconKey { get; private set; } = string.Empty;
}
