namespace BC_Progress;

internal sealed class CampaignProgressRow
{
    public CampaignProgressRow(CampaignProgressEntry entry)
    {
        Campaign = entry.Campaign;
        Chapter = entry.Chapter;
        Stage = entry.Stage;
    }

    public string Campaign { get; set; }

    public string Chapter { get; set; }

    public string Stage { get; set; }
}
