using Bloodcraft.Patches;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using System.Globalization;
using System.Text;
using Unity.Entities;
using static Bloodcraft.Services.EclipseInterface;
using static Bloodcraft.Services.EclipseService;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;

namespace Bloodcraft.Services;
internal class EclipseInterface
{
    public static readonly bool ExtraRecipes = ConfigService.ExtraRecipes;
    public static readonly int PrimalCost = ConfigService.PrimalJewelCost;
    public static readonly int MaxLevel = ConfigService.MaxLevel;
    public static readonly int MaxLegacyLevel = ConfigService.MaxBloodLevel;
    public static readonly int MaxExpertiseLevel = ConfigService.MaxExpertiseLevel;
    public static readonly int MaxFamiliarLevel = ConfigService.MaxFamiliarLevel;
    public static readonly float PrestigeStatMultiplier = ConfigService.PrestigeStatMultiplier;
    public static readonly float ClassStatMultiplier = ConfigService.StatSynergyMultiplier;

    public const int MAX_PROFESSION_LEVEL = 100;
}
public interface IVersionHandler<TProgressData>
{
    void SendClientConfig(User user);
    void SendClientProgress(Entity character, ulong steamId);
    string BuildConfigMessage();
    string BuildProgressMessage(TProgressData data);
}
public static class VersionHandler
{
    public static readonly Dictionary<string, object> VersionHandlers = new()
    {
        { "1.1.2", new VersionHandler_1_1_2() },
        { "1.2.2", new VersionHandler_1_2_2() },
        { "1.3.2", new VersionHandler_1_3_2() }
    };

#nullable enable
    public static IVersionHandler<TProgressData>? GetHandler<TProgressData>(string version)
    {
        if (VersionHandlers.TryGetValue(version, out var handler) && handler is IVersionHandler<TProgressData> typedHandler)
        {
            return typedHandler;
        }

        return null;
    }

#nullable disable
}
public class VersionHandler_1_1_2 : IVersionHandler<ProgressDataV1_1_2>
{
    public void SendClientConfig(User user)
    {
        string message = BuildConfigMessage();
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMACV1_1_2(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public void SendClientProgress(Entity character, ulong steamId)
    {
        Entity userEntity = character.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();

        ProgressDataV1_1_2 data = new()
        {
            ExperienceData = GetExperienceData(steamId),
            LegacyData = GetLegacyData(character, steamId),
            ExpertiseData = GetExpertiseData(character, steamId),
            DailyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Daily),
            WeeklyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Weekly)
        };

        string message = BuildProgressMessage(data);
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMACV1_1_2(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public string BuildConfigMessage()
    {
        List<float> weaponStatValues = Enum.GetValues(typeof(WeaponStatType)).Cast<WeaponStatType>().Select(stat => WeaponStatValues[stat]).ToList();
        List<float> bloodStatValues = Enum.GetValues(typeof(BloodStatType)).Cast<BloodStatType>().Select(stat => BloodStatValues[stat]).ToList();

        float prestigeStatMultiplier = PrestigeStatMultiplier;
        float statSynergyMultiplier = ClassStatMultiplier;

        int maxPlayerLevel = MaxLevel;
        int maxLegacyLevel = MaxLegacyLevel;
        int maxExpertiseLevel = MaxExpertiseLevel;

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ConfigsToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:F2},{1:F2},{2},{3},{4},", prestigeStatMultiplier, statSynergyMultiplier, maxPlayerLevel, maxLegacyLevel, maxExpertiseLevel); // Add multipliers to the message

        sb.Append(string.Join(",", weaponStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        sb.Append(string.Join(",", bloodStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        foreach (var classEntry in Classes.ClassWeaponBloodEnumMap)
        {
            var playerClass = classEntry.Key;
            var (weaponSynergies, bloodSynergies) = classEntry.Value;

            sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D2},", (int)playerClass + 1);
            sb.Append(string.Join("", weaponSynergies.Select(s => (s + 1).ToString("D2"))));
            sb.Append(',');

            sb.Append(string.Join("", bloodSynergies.Select(s => (s + 1).ToString("D2"))));
            sb.Append(',');
        }

        if (sb[^1] == ',')
            sb.Length--;

        return sb.ToString();
    }
    public string BuildProgressMessage(ProgressDataV1_1_2 data)
    {
        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ProgressToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},", data.ExperienceData.Percent, data.ExperienceData.Level, data.ExperienceData.Prestige, data.ExperienceData.Class)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", data.LegacyData.Percent, data.LegacyData.Level, data.LegacyData.Prestige, data.LegacyData.Enum, data.LegacyData.LegacyBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", data.ExpertiseData.Percent, data.ExpertiseData.Level, data.ExpertiseData.Prestige, data.ExpertiseData.Enum, data.ExpertiseData.ExpertiseBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4},", data.DailyQuestData.Type, data.DailyQuestData.Progress, data.DailyQuestData.Goal, data.DailyQuestData.Target, data.DailyQuestData.IsVBlood)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4}", data.WeeklyQuestData.Type, data.WeeklyQuestData.Progress, data.WeeklyQuestData.Goal, data.WeeklyQuestData.Target, data.WeeklyQuestData.IsVBlood);

        return sb.ToString();
    }
}
public class VersionHandler_1_2_2 : IVersionHandler<ProgressDataV1_2_2>
{
    public void SendClientConfig(User user)
    {
        string message = BuildConfigMessage();
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMACV1_2_2(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public void SendClientProgress(Entity character, ulong steamId)
    {
        Entity userEntity = character.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();

        ProgressDataV1_2_2 data = new()
        {
            ExperienceData = GetExperienceData(steamId),
            LegacyData = GetLegacyData(character, steamId),
            ExpertiseData = GetExpertiseData(character, steamId),
            FamiliarData = GetFamiliarData(character, steamId),
            ProfessionData = GetProfessionData(steamId),
            DailyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Daily),
            WeeklyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Weekly)
        };

        string message = BuildProgressMessage(data);
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMACV1_2_2(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public string BuildConfigMessage()
    {
        List<float> weaponStatValues = Enum.GetValues(typeof(WeaponStatType)).Cast<WeaponStatType>().Select(stat => WeaponStatValues[stat]).ToList();
        List<float> bloodStatValues = Enum.GetValues(typeof(BloodStatType)).Cast<BloodStatType>().Select(stat => BloodStatValues[stat]).ToList();

        float prestigeStatMultiplier = PrestigeStatMultiplier;
        float statSynergyMultiplier = ClassStatMultiplier;

        int maxPlayerLevel = MaxLevel;
        int maxLegacyLevel = MaxLegacyLevel;
        int maxExpertiseLevel = MaxExpertiseLevel;
        int maxFamiliarLevel = MaxFamiliarLevel;
        int maxProfessionLevel = MAX_PROFESSION_LEVEL;

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ConfigsToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:F2},{1:F2},{2},{3},{4},{5},{6},", prestigeStatMultiplier, statSynergyMultiplier, maxPlayerLevel, maxLegacyLevel, maxExpertiseLevel, maxFamiliarLevel, maxProfessionLevel); // Add multipliers to the message

        sb.Append(string.Join(",", weaponStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        sb.Append(string.Join(",", bloodStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        foreach (var classEntry in Classes.ClassWeaponBloodEnumMap)
        {
            var playerClass = classEntry.Key;
            var (weaponSynergies, bloodSynergies) = classEntry.Value;

            sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D2},", (int)playerClass + 1);
            sb.Append(string.Join("", weaponSynergies.Select(s => (s + 1).ToString("D2"))));
            sb.Append(',');

            sb.Append(string.Join("", bloodSynergies.Select(s => (s + 1).ToString("D2"))));
            sb.Append(',');
        }

        if (sb[^1] == ',')
            sb.Length--;

        return sb.ToString();
    }
    public string BuildProgressMessage(ProgressDataV1_2_2 data)
    {
        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ProgressToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},", data.ExperienceData.Percent, data.ExperienceData.Level, data.ExperienceData.Prestige, data.ExperienceData.Class)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", data.LegacyData.Percent, data.LegacyData.Level, data.LegacyData.Prestige, data.LegacyData.Enum, data.LegacyData.LegacyBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", data.ExpertiseData.Percent, data.ExpertiseData.Level, data.ExpertiseData.Prestige, data.ExpertiseData.Enum, data.ExpertiseData.ExpertiseBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},{4},", data.FamiliarData.Percent, data.FamiliarData.Level, data.FamiliarData.Prestige, data.FamiliarData.Name, data.FamiliarData.FamiliarStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D2},{5:D2},{6:D2},{7:D2},{8:D2},{9:D2},{10:D2},{11:D2},{12:D2},{13:D2},{14:D2},{15:D2},",
                data.ProfessionData.EnchantingProgress, data.ProfessionData.EnchantingLevel, data.ProfessionData.AlchemyProgress, data.ProfessionData.AlchemyLevel,
                data.ProfessionData.HarvestingProgress, data.ProfessionData.HarvestingLevel, data.ProfessionData.BlacksmithingProgress, data.ProfessionData.BlacksmithingLevel,
                data.ProfessionData.TailoringProgress, data.ProfessionData.TailoringLevel, data.ProfessionData.WoodcuttingProgress, data.ProfessionData.WoodcuttingLevel,
                data.ProfessionData.MiningProgress, data.ProfessionData.MiningLevel, data.ProfessionData.FishingProgress, data.ProfessionData.FishingLevel)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4},", data.DailyQuestData.Type, data.DailyQuestData.Progress, data.DailyQuestData.Goal, data.DailyQuestData.Target, data.DailyQuestData.IsVBlood)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4}", data.WeeklyQuestData.Type, data.WeeklyQuestData.Progress, data.WeeklyQuestData.Goal, data.WeeklyQuestData.Target, data.WeeklyQuestData.IsVBlood);

        return sb.ToString();
    }
}
public class VersionHandler_1_3_2 : IVersionHandler<ProgressDataV1_3_2>
{
    public void SendClientConfig(User user)
    {
        string message = BuildConfigMessage();
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMACV1_3_2(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public void SendClientProgress(Entity playerCharacter, ulong steamId)
    {
        Entity userEntity = playerCharacter.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();

        ProgressDataV1_3_2 data = new()
        {
            ExperienceData = GetExperienceData(steamId),
            LegacyData = GetLegacyData(playerCharacter, steamId),
            ExpertiseData = GetExpertiseData(playerCharacter, steamId),
            FamiliarData = GetFamiliarData(playerCharacter, steamId),
            ProfessionData = GetProfessionData(steamId),
            DailyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Daily),
            WeeklyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Weekly),
            ShiftSpellData = GetShiftSpellData(playerCharacter)
        };

        string message = BuildProgressMessage(data);
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMACV1_3_2(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
        // LocalizationService.SendToClient(playerCharacter, userEntity, messageWithMAC);
    }
    /*
    public string BuildConfigMessage()
    {
        List<float> weaponStatValues = Enum.GetValues(typeof(WeaponStatType)).Cast<WeaponStatType>().Select(stat => WeaponStatValues[stat]).ToList();
        List<float> bloodStatValues = Enum.GetValues(typeof(BloodStatType)).Cast<BloodStatType>().Select(stat => BloodStatValues[stat]).ToList();

        float prestigeStatMultiplier = PrestigeStatMultiplier;
        float statSynergyMultiplier = ClassStatMultiplier;

        int maxPlayerLevel = MaxLevel;
        int maxLegacyLevel = MaxLegacyLevel;
        int maxExpertiseLevel = MaxExpertiseLevel;
        int maxFamiliarLevel = MaxFamiliarLevel;
        int maxProfessionLevel = MAX_PROFESSION_LEVEL; // no longer need to send this but changing everything involved that would need to be cut/changed too much extra work atm

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ConfigsToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:F2},{1:F2},{2},{3},{4},{5},{6},", prestigeStatMultiplier, statSynergyMultiplier, maxPlayerLevel, maxLegacyLevel, 
            maxExpertiseLevel, maxFamiliarLevel, maxProfessionLevel);

        sb.Append(string.Join(",", weaponStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        sb.Append(string.Join(",", bloodStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        foreach (var classEntry in Classes.ClassWeaponBloodEnumMap)
        {
            var playerClass = classEntry.Key;
            var (weaponSynergies, bloodSynergies) = classEntry.Value;

            sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D2},", (int)playerClass + 1);
            sb.Append(string.Join("", weaponSynergies.Select(s => (s + 1).ToString("D2"))));
            sb.Append(',');

            sb.Append(string.Join("", bloodSynergies.Select(s => (s + 1).ToString("D2"))));
            sb.Append(',');
        }

        if (sb[^1] == ',')
            sb.Length--;

        return sb.ToString();
    }
    */
    public string BuildConfigMessage()
    {
        List<float> weaponStatValues = Enum.GetValues(typeof(WeaponStatType)).Cast<WeaponStatType>().Select(stat => WeaponStatValues[stat]).ToList();
        List<float> bloodStatValues = Enum.GetValues(typeof(BloodStatType)).Cast<BloodStatType>().Select(stat => BloodStatValues[stat]).ToList();

        float prestigeStatMultiplier = PrestigeStatMultiplier;
        float statSynergyMultiplier = ClassStatMultiplier;

        int maxPlayerLevel = MaxLevel;
        int maxLegacyLevel = MaxLegacyLevel;
        int maxExpertiseLevel = MaxExpertiseLevel;
        int maxFamiliarLevel = MaxFamiliarLevel;
        int maxProfessionLevel = MAX_PROFESSION_LEVEL; // no longer need to send this but changing everything involved that would need to be cut/changed too much extra work atm
        bool extraRecipes = ExtraRecipes;
        int primalCost = PrimalCost;

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ConfigsToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:F2},{1:F2},{2},{3},{4},{5},{6},{7},{8},", prestigeStatMultiplier, statSynergyMultiplier, maxPlayerLevel, maxLegacyLevel,
            maxExpertiseLevel, maxFamiliarLevel, maxProfessionLevel, extraRecipes, primalCost);

        sb.Append(string.Join(",", weaponStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        sb.Append(string.Join(",", bloodStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        foreach (var classEntry in Classes.ClassWeaponBloodEnumMap)
        {
            var playerClass = classEntry.Key;
            var (weaponSynergies, bloodSynergies) = classEntry.Value;

            sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D2},", (int)playerClass + 1);
            sb.Append(string.Join("", weaponSynergies.Select(s => (s + 1).ToString("D2"))));
            sb.Append(',');

            sb.Append(string.Join("", bloodSynergies.Select(s => (s + 1).ToString("D2"))));
            sb.Append(',');
        }

        if (sb[^1] == ',')
            sb.Length--;

        return sb.ToString();
    }
    public string BuildProgressMessage(ProgressDataV1_3_2 data)
    {
        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ProgressToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},", data.ExperienceData.Percent, data.ExperienceData.Level, data.ExperienceData.Prestige, data.ExperienceData.Class)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", data.LegacyData.Percent, data.LegacyData.Level, data.LegacyData.Prestige, data.LegacyData.Enum, data.LegacyData.LegacyBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", data.ExpertiseData.Percent, data.ExpertiseData.Level, data.ExpertiseData.Prestige, data.ExpertiseData.Enum, data.ExpertiseData.ExpertiseBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},{4},", data.FamiliarData.Percent, data.FamiliarData.Level, data.FamiliarData.Prestige, data.FamiliarData.Name, data.FamiliarData.FamiliarStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D2},{5:D2},{6:D2},{7:D2},{8:D2},{9:D2},{10:D2},{11:D2},{12:D2},{13:D2},{14:D2},{15:D2},",
                data.ProfessionData.EnchantingProgress, data.ProfessionData.EnchantingLevel, data.ProfessionData.AlchemyProgress, data.ProfessionData.AlchemyLevel,
                data.ProfessionData.HarvestingProgress, data.ProfessionData.HarvestingLevel, data.ProfessionData.BlacksmithingProgress, data.ProfessionData.BlacksmithingLevel,
                data.ProfessionData.TailoringProgress, data.ProfessionData.TailoringLevel, data.ProfessionData.WoodcuttingProgress, data.ProfessionData.WoodcuttingLevel,
                data.ProfessionData.MiningProgress, data.ProfessionData.MiningLevel, data.ProfessionData.FishingProgress, data.ProfessionData.FishingLevel)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4},", data.DailyQuestData.Type, data.DailyQuestData.Progress, data.DailyQuestData.Goal, data.DailyQuestData.Target, data.DailyQuestData.IsVBlood)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4},", data.WeeklyQuestData.Type, data.WeeklyQuestData.Progress, data.WeeklyQuestData.Goal, data.WeeklyQuestData.Target, data.WeeklyQuestData.IsVBlood)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2}", data.ShiftSpellData);

        return sb.ToString();
    }
}
public class ProgressDataV1_1_2
{
    public (int Percent, int Level, int Prestige, int Class) ExperienceData { get; set; }
    public (int Percent, int Level, int Prestige, int Enum, int LegacyBonusStats) LegacyData { get; set; }
    public (int Percent, int Level, int Prestige, int Enum, int ExpertiseBonusStats) ExpertiseData { get; set; }
    public (int Type, int Progress, int Goal, string Target, string IsVBlood) DailyQuestData { get; set; }
    public (int Type, int Progress, int Goal, string Target, string IsVBlood) WeeklyQuestData { get; set; }
}
public class ProgressDataV1_2_2
{
    public (int Percent, int Level, int Prestige, int Class) ExperienceData { get; set; }
    public (int Percent, int Level, int Prestige, int Enum, int LegacyBonusStats) LegacyData { get; set; }
    public (int Percent, int Level, int Prestige, int Enum, int ExpertiseBonusStats) ExpertiseData { get; set; }
    public (int Percent, int Level, int Prestige, string Name, string FamiliarStats) FamiliarData { get; set; }

    public (int EnchantingProgress, int EnchantingLevel, int AlchemyProgress, int AlchemyLevel,
        int HarvestingProgress, int HarvestingLevel, int BlacksmithingProgress, int BlacksmithingLevel,
        int TailoringProgress, int TailoringLevel, int WoodcuttingProgress, int WoodcuttingLevel,
        int MiningProgress, int MiningLevel, int FishingProgress, int FishingLevel) ProfessionData
    { get; set; }
    public (int Type, int Progress, int Goal, string Target, string IsVBlood) DailyQuestData { get; set; }
    public (int Type, int Progress, int Goal, string Target, string IsVBlood) WeeklyQuestData { get; set; }
}
public class ProgressDataV1_3_2
{
    public (int Percent, int Level, int Prestige, int Class) ExperienceData { get; set; }
    public (int Percent, int Level, int Prestige, int Enum, int LegacyBonusStats) LegacyData { get; set; }
    public (int Percent, int Level, int Prestige, int Enum, int ExpertiseBonusStats) ExpertiseData { get; set; }
    public (int Percent, int Level, int Prestige, string Name, string FamiliarStats) FamiliarData { get; set; }

    public (int EnchantingProgress, int EnchantingLevel, int AlchemyProgress, int AlchemyLevel,
        int HarvestingProgress, int HarvestingLevel, int BlacksmithingProgress, int BlacksmithingLevel,
        int TailoringProgress, int TailoringLevel, int WoodcuttingProgress, int WoodcuttingLevel,
        int MiningProgress, int MiningLevel, int FishingProgress, int FishingLevel) ProfessionData
    { get; set; }
    public (int Type, int Progress, int Goal, string Target, string IsVBlood) DailyQuestData { get; set; }
    public (int Type, int Progress, int Goal, string Target, string IsVBlood) WeeklyQuestData { get; set; }
    public int ShiftSpellData { get; set; }
}