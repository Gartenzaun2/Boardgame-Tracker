namespace BoardgameTracker.Application.History.Transfer;

/// <summary>One fully validated, serialized export, independent of its destination.</summary>
public sealed record BgtExportContent(string FileName, ReadOnlyMemory<byte> Bytes);
