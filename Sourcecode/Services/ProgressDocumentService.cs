using System.Text.Json;

namespace BC_Progress;

internal sealed class ProgressDocumentService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ProgressDocumentService(JsonSerializerOptions jsonOptions)
    {
        _jsonOptions = jsonOptions;
    }

    public ProgressDocument Load(string path)
    {
        if (!File.Exists(path))
        {
            var created = CreateDefaultDocument();
            Save(path, created);
            return created;
        }

        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<ProgressDocument>(json, _jsonOptions) ?? CreateDefaultDocument();
        Normalize(document);
        return document;
    }

    public void Save(string path, ProgressDocument document)
    {
        Normalize(document);
        document.Profile!.LastUpdated = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

        var json = JsonSerializer.Serialize(document, _jsonOptions);
        File.WriteAllText(path, json);
    }

    public ProgressDocument ImportFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var imported = JsonSerializer.Deserialize<ProgressDocument>(json, _jsonOptions);
        if (imported is null)
        {
            throw new InvalidDataException("The selected file is not a valid progress file.");
        }

        Normalize(imported);
        return imported;
    }

    public void ExportToFile(string filePath, ProgressDocument document)
    {
        Normalize(document);
        document.Profile!.LastUpdated = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

        var json = JsonSerializer.Serialize(document, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static string ResolveProgressFilePath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "Progress.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "Progress.json");
    }

    public static List<CampaignProgressEntry> CreateDefaultCampaignProgress()
    {
        return
        [
            new CampaignProgressEntry { Campaign = "Empire of Cats", Chapter = "Chapter 1", Stage = string.Empty },
            new CampaignProgressEntry { Campaign = "Empire of Cats", Chapter = "Chapter 2", Stage = string.Empty },
            new CampaignProgressEntry { Campaign = "Empire of Cats", Chapter = "Chapter 3", Stage = string.Empty },
            new CampaignProgressEntry { Campaign = "Into the Future", Chapter = "Chapter 1", Stage = string.Empty },
            new CampaignProgressEntry { Campaign = "Into the Future", Chapter = "Chapter 2", Stage = string.Empty },
            new CampaignProgressEntry { Campaign = "Into the Future", Chapter = "Chapter 3", Stage = string.Empty },
            new CampaignProgressEntry { Campaign = "Cats of the Cosmos", Chapter = "Chapter 1", Stage = string.Empty },
            new CampaignProgressEntry { Campaign = "Cats of the Cosmos", Chapter = "Chapter 2", Stage = string.Empty },
            new CampaignProgressEntry { Campaign = "Cats of the Cosmos", Chapter = "Chapter 3", Stage = string.Empty },
            new CampaignProgressEntry { Campaign = "Story of Legends", Chapter = "General", Stage = string.Empty }
        ];
    }

    public static ProgressDocument CreateDefaultDocument()
    {
        return new ProgressDocument
        {
            Profile = new ProfileInfo
            {
                Game = "The Battle Cats",
                FormatVersion = "1.0",
                LastUpdated = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")
            },
            Categories = new Dictionary<string, List<CatEntry>>(),
            CampaignProgress = CreateDefaultCampaignProgress()
        };
    }

    private static void Normalize(ProgressDocument document)
    {
        document.Profile ??= new ProfileInfo();
        document.Categories ??= new Dictionary<string, List<CatEntry>>();
        document.CampaignProgress ??= CreateDefaultCampaignProgress();
    }
}
