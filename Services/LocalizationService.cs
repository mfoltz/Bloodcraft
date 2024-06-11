using Stunlock.Core;
using System.Reflection;
using System.Text.Json;

namespace Bloodcraft.Services;

public class LocalizationService
{
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

    public LocalizationService()
    {
        LoadLocalization();
        LoadPrefabNames();
    }

    void LoadLocalization()
    {
        var resourceName = "Bloodcraft.Localization.English.json";
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

    public string GetLocalization(string guid)
    {
        if (localization.TryGetValue(guid, out var text))
        {
            return text;
        }
        return $"<Localization not found for {guid}>";
    }

}
