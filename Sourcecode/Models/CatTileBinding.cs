namespace BC_Progress;

internal sealed class CatTileBinding
{
    public CatTileBinding(string category, CatEntry entry)
    {
        Category = category;
        Entry = entry;
    }

    public string Category { get; }

    public CatEntry Entry { get; }
}
