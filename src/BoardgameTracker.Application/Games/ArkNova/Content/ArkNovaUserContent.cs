namespace BoardgameTracker.Application.Games.ArkNova.Content;

// Only names are currently editable content. Symbols remain on the domain card.
public sealed class ArkNovaUserContent
{
    public const int MaximumNameLength = 120;
    public const string SpecialCard = "special-card";
    public const string ZooPlan = "zoo-plan";
    private ArkNovaUserContent() { }
    public ArkNovaUserContent(string resourceType, string objectId, string name)
    {
        if (resourceType == SpecialCard)
        {
            if (!Guid.TryParseExact(objectId, "D", out Guid id) || id == Guid.Empty || id.ToString() != objectId)
                throw new ArgumentException("Invalid special-card identity.", nameof(objectId));
        }
        else if (resourceType == ZooPlan)
        {
            if (!int.TryParse(objectId, out int number) || number is < 1 or > 14 || number.ToString() != objectId)
                throw new ArgumentException("Invalid zoo-plan number.", nameof(objectId));
        }
        else
            throw new ArgumentException("Unknown Ark Nova resource type.", nameof(resourceType));
        ResourceType = resourceType;
        ObjectId = objectId;
        SetName(name);
    }
    public string ResourceType { get; private set; } = string.Empty;
    public string ObjectId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > MaximumNameLength)
            throw new ArgumentException("The content name is too long.", nameof(name));
        Name = name.Trim();
    }
}
