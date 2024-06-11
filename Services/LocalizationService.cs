using Stunlock.Core;
using System.Reflection;
using System.Text.Json;

namespace Bloodcraft.Services;

public class LocalizationService
{

    static readonly string LanguageLocalization = Plugin.LanguageLocalization.Value;
    struct Code
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }

    struct Node
    {
        public string Guid { get; set; }
        public string Text { get; set; }
    }

    struct LocalizationFile
    {
        public Code[] Codes { get; set; }
        public Node[] Nodes { get; set; }
    }

    public Dictionary<string, string> localization = [];
    public Dictionary<int, string> prefabNames = [];

    static readonly Dictionary<string, string> LocalizationMapping = new()
    {
        {"English", "Bloodcraft.Localization.English.json"},
        {"German", "Bloodcraft.Localization.German.json"},
        {"French", "Bloodcraft.Localization.French.json"},
        {"Spanish", "Bloodcraft.Localization.Spanish.json"},
        {"Italian", "Bloodcraft.Localization.Italian.json"},
        {"Japanese", "Bloodcraft.Localization.Japanese.json"},
        {"Koreana", "Bloodcraft.Localization.Koreana.json"},
        {"Portuguese", "Bloodcraft.Localization.Portuguese.json"},
        {"Russian", "Bloodcraft.Localization.Russian.json"},
        {"SimplifiedChinese", "Bloodcraft.Localization.SChinese.json"},
        {"TraditionalChinese", "Bloodcraft.Localization.TChinese.json"},
        {"Hungarian", "Bloodcraft.Localization.Hungarian.json"},
        {"Latam", "Bloodcraft.Localization.Latam.json"},
        {"Polish", "Bloodcraft.Localization.Polish.json"},
        {"Thai", "Bloodcraft.Localization.Thai.json"},
        {"Turkish", "Bloodcraft.Localization.Turkish.json"},
        {"Vietnamese", "Bloodcraft.Localization.Vietnamese.json"},
        {"Brazilian", "Bloodcraft.Localization.Brazilian.json"}
    };
    public LocalizationService()
    {
        LoadLocalization();
        LoadPrefabNames();
    }

    void LoadLocalization()
    {
        var resourceName = LocalizationMapping.ContainsKey(LanguageLocalization) ? LocalizationMapping[LanguageLocalization] : "Bloodcraft.Localization.English.json";

        //Core.Log.LogInfo($"Loading localization file: {resourceName}");

        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);
        
        using StreamReader reader = new(stream);
        string jsonContent = reader.ReadToEnd();
        var localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent);
        localization = localizationFile.Nodes.ToDictionary(x => x.Guid, x => x.Text);   
    }

    void LoadPrefabNames()
    {
        var resourceName = "Bloodcraft.Localization.Prefabs.json";
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);

        using StreamReader reader = new(stream);
        string jsonContent = reader.ReadToEnd();
        prefabNames = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonContent);
    }

    public string GetLocalization(string Guid)
    {
        if (localization.TryGetValue(Guid, out var Text))
        {
            return Text;
        }
        return $"<Localization not found for {Guid}>";
    }

}
