namespace BC_Progress;

internal sealed class EvolutionInfo
{
    public EvolutionInfo(string pageTitle, string? nextEvolutionName)
    {
        PageTitle = pageTitle;
        NextEvolutionName = nextEvolutionName;
    }

    public string PageTitle { get; }

    public string? NextEvolutionName { get; }
}
