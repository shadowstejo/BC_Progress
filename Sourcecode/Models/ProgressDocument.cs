namespace BC_Progress;

internal sealed class ProgressDocument
{
    public ProfileInfo? Profile { get; set; }

    public Dictionary<string, List<CatEntry>> Categories { get; set; } = new();

    public List<CampaignProgressEntry>? CampaignProgress { get; set; }
}

internal sealed class ProfileInfo
{
    public string Game { get; set; } = string.Empty;

    public string FormatVersion { get; set; } = string.Empty;

    public string LastUpdated { get; set; } = string.Empty;
}

internal sealed class CatEntry
{
    public string Name { get; set; } = string.Empty;

    public int? LevelBase { get; set; }

    public int? LevelPlus { get; set; }

    public bool Owned { get; set; }

    public bool LevelUnknown { get; set; }

    public string? Notes { get; set; }

    public int? ViewOrder { get; set; }
}

internal sealed class CampaignProgressEntry
{
    public string Campaign { get; set; } = string.Empty;

    public string Chapter { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;
}
