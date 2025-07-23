using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Bloodcraft.Interfaces.BloodLegacyManager;
using static Bloodcraft.Interfaces.BloodLegacyManager.BloodStatsManager;
using static Bloodcraft.Interfaces.LevelingManager;
using static Bloodcraft.Interfaces.PrestigeManager;
using static Bloodcraft.Interfaces.ProfessionManager;
using static Bloodcraft.Interfaces.SpellsManager;
using static Bloodcraft.Interfaces.WeaponExpertiseManager;
using static Bloodcraft.Interfaces.WeaponExpertiseManager.WeaponStatManager;

namespace Bloodcraft.Interfaces;
internal interface IDataManager
{
    void SaveAll();
    void LoadAll();
    void Save(ulong steamId);
    void Load(ulong steamId);
}
internal abstract class DataManager<T> : IDataManager where T : class, new()
{
    protected static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };
    protected abstract string FolderName { get; }
    protected abstract Dictionary<ulong, T> DataMap { get; }
    protected virtual string FileName => typeof(T).Name + ".json";
    protected string GetPlayerPath(ulong steamId)
    {
        string folder = Path.Combine(BasePath, FolderName, steamId.ToString());
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, FileName);
    }
    public virtual void Save(ulong steamId)
    {
        if (!DataMap.TryGetValue(steamId, out T data)) return;
        string path = GetPlayerPath(steamId);
        string json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText(path, json);
    }
    public virtual void Load(ulong steamId)
    {
        string path = GetPlayerPath(steamId);
        if (!File.Exists(path)) return;
        string json = File.ReadAllText(path);
        DataMap[steamId] = JsonSerializer.Deserialize<T>(json, _options) ?? new T();
    }
    public void SaveAll()
    {
        foreach (var steamId in DataMap.Keys)
        {
            Save(steamId);
        }
    }
    public void LoadAll()
    {
        // Optional: Directory scanning logic if you want to bulk-load all existing files
    }
    protected static string BasePath => Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
}
internal class LevelingManager : DataManager<LevelingData>
{
    public class LevelingData
    {
        public KeyValuePair<int, float> Experience { get; set; }
        public KeyValuePair<DateTime, float> RestedXP { get; set; }
    }
    static readonly Dictionary<ulong, LevelingData> _data = [];
    protected override string FolderName => "Leveling";
    protected override Dictionary<ulong, LevelingData> DataMap => _data;
}
internal class PrestigeManager : DataManager<PrestigeData>
{
    public class PrestigeData
    {
        public Dictionary<PrestigeType, int> Prestiges { get; set; } = [];
    }
    static readonly Dictionary<ulong, PrestigeData> _data = [];
    protected override string FolderName => "Prestige";
    protected override Dictionary<ulong, PrestigeData> DataMap => _data;
}
internal class ProfessionManager : DataManager<ProfessionData>
{
    public class ProfessionData
    {
        public Dictionary<Profession, KeyValuePair<int, float>> Professions { get; set; } = [];
    }
    static readonly Dictionary<ulong, ProfessionData> _data = [];
    protected override string FolderName => "Professions";
    protected override Dictionary<ulong, ProfessionData> DataMap => _data;
    public static KeyValuePair<int, float> GetProfessionXP(ulong steamId, Profession type)
    {
        if (!_data.TryGetValue(steamId, out var data)) return new(0, 0f);
        return data.Professions.TryGetValue(type, out var xp) ? xp : new(0, 0f);
    }
    public void SetProfessionXP(ulong steamId, Profession type, KeyValuePair<int, float> value)
    {
        if (!_data.TryGetValue(steamId, out var data))
        {
            data = new ProfessionData();
            _data[steamId] = data;
        }
        data.Professions[type] = value;
        Save(steamId);
    }
}
internal class BloodLegacyManager : DataManager<BloodLegacyData>
{
    public class BloodLegacyData
    {
        public Dictionary<BloodType, KeyValuePair<int, float>> Legacies { get; set; } = [];
    }
    static readonly Dictionary<ulong, BloodLegacyData> _data = [];
    protected override string FolderName => "BloodLegacies";
    protected override Dictionary<ulong, BloodLegacyData> DataMap => _data;
    public static KeyValuePair<int, float> GetLegacyXP(ulong steamId, BloodType type)
    {
        if (!_data.TryGetValue(steamId, out var data)) return new(0, 0f);
        return data.Legacies.TryGetValue(type, out var xp) ? xp : new(0, 0f);
    }
    public void SetLegacyXP(ulong steamId, BloodType type, KeyValuePair<int, float> value)
    {
        if (!_data.TryGetValue(steamId, out var data))
        {
            data = new BloodLegacyData();
            _data[steamId] = data;
        }
        data.Legacies[type] = value;
        Save(steamId);
    }
    public class BloodStatsManager : DataManager<BloodTypeStatsData>
    {
        public class BloodTypeStatsData
        {
            public Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>> BloodTypeStats { get; set; } = [];
        }

        static readonly Dictionary<ulong, BloodTypeStatsData> _data = [];
        protected override string FolderName => "BloodLegacies";
        protected override Dictionary<ulong, BloodTypeStatsData> DataMap => _data;
        public static List<BloodManager.BloodStats.BloodStatType> GetStats(ulong steamId, BloodType type)
        {
            if (!_data.TryGetValue(steamId, out var data)) return [];
            return data.BloodTypeStats.TryGetValue(type, out var stats) ? stats : [];
        }
        public void SetStats(ulong steamId, BloodType type, List<BloodManager.BloodStats.BloodStatType> stats)
        {
            if (!_data.TryGetValue(steamId, out var data))
            {
                data = new BloodTypeStatsData();
                _data[steamId] = data;
            }

            data.BloodTypeStats[type] = stats;
            Save(steamId);
        }
    }
}
internal class WeaponExpertiseManager : DataManager<WeaponExpertiseData>
{
    public class WeaponExpertiseData
    {
        public Dictionary<WeaponType, KeyValuePair<int, float>> WeaponXP { get; set; } = [];
    }

    static readonly Dictionary<ulong, WeaponExpertiseData> _data = [];
    protected override string FolderName => "WeaponExpertise";
    protected override Dictionary<ulong, WeaponExpertiseData> DataMap => _data;
    public static KeyValuePair<int, float> GetExpertise(ulong steamId, WeaponType type)
    {
        if (!_data.TryGetValue(steamId, out var data)) return new(0, 0f);
        return data.WeaponXP.TryGetValue(type, out var xp) ? xp : new(0, 0f);
    }
    public void SetExpertise(ulong steamId, WeaponType type, KeyValuePair<int, float> value)
    {
        if (!_data.TryGetValue(steamId, out var data))
        {
            data = new WeaponExpertiseData();
            _data[steamId] = data;
        }
        data.WeaponXP[type] = value;
        Save(steamId);
    }
    public class WeaponStatManager : DataManager<WeaponTypeStatsData>
    {
        public class WeaponTypeStatsData
        {
            public Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>> WeaponTypeStats { get; set; } = [];
        }
        static readonly Dictionary<ulong, WeaponTypeStatsData> _data = [];
        protected override string FolderName => "WeaponExpertise";
        protected override Dictionary<ulong, WeaponTypeStatsData> DataMap => _data;
        public static List<WeaponManager.WeaponStats.WeaponStatType> GetStats(ulong steamId, WeaponType type)
        {
            if (!_data.TryGetValue(steamId, out var data)) return [];
            return data.WeaponTypeStats.TryGetValue(type, out var stats) ? stats : [];
        }
        public void SetStats(ulong steamId, WeaponType type, List<WeaponManager.WeaponStats.WeaponStatType> stats)
        {
            if (!_data.TryGetValue(steamId, out var data))
            {
                data = new WeaponTypeStatsData();
                _data[steamId] = data;
            }

            data.WeaponTypeStats[type] = stats;
            Save(steamId);
        }
    }
}
internal class SpellsManager : DataManager<SpellsData>
{
    public class SpellsData
    {
        public (int FirstUnarmed, int SecondUnarmed, int ClassSpell) Spells;
    }
    static readonly Dictionary<ulong, SpellsData> _data = [];
    protected override string FolderName => "Spells";
    protected override Dictionary<ulong, SpellsData> DataMap => _data;
    public static (int, int, int) GetSpells(ulong steamId)
    {
        if (!_data.TryGetValue(steamId, out var data)) return (0, 0, 0);
        return data.Spells;
    }
    public void SetSpells(ulong steamId, (int, int, int) spells)
    {
        if (!_data.TryGetValue(steamId, out var data))
        {
            data = new SpellsData();
            _data[steamId] = data;
        }
        data.Spells = spells;
        Save(steamId);
    }
}