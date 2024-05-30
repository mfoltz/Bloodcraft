namespace Bloodcraft.Systems.Leveling;

public class PrestigeSystem
{
    public enum PrestigeType
    {
        Experience,
        SwordExpertise,
        AxeExpertise,
        MaceExpertise,
        SpearExpertise,
        CrossbowExpertise,
        GreatSwordExpertise,
        SlashersExpertise,
        PistolsExpertise,
        ReaperExpertise,
        LongbowExpertise,
        WhipExpertise,
        Sanguimancy,
        WorkerLegacy,
        WarriorLegacy,
        ScholarLegacy,
        RogueLegacy,
        MutantLegacy,
        VBloodLegacy,
        DraculinLegacy,
        ImmortalLegacy,
        CreatureLegacy,
        BruteLegacy
    }
    static readonly Dictionary<PrestigeSystem.PrestigeType, Dictionary<ulong, KeyValuePair<int, float>>> prestigeTypeToPlayerDataMap = new()
    {
        { PrestigeSystem.PrestigeType.Experience, Core.DataStructures.PlayerExperience },
        { PrestigeSystem.PrestigeType.SwordExpertise, Core.DataStructures.PlayerSwordExpertise },
        { PrestigeSystem.PrestigeType.AxeExpertise, Core.DataStructures.PlayerAxeExpertise },
        { PrestigeSystem.PrestigeType.MaceExpertise, Core.DataStructures.PlayerMaceExpertise },
        { PrestigeSystem.PrestigeType.SpearExpertise, Core.DataStructures.PlayerSpearExpertise },
        { PrestigeSystem.PrestigeType.CrossbowExpertise, Core.DataStructures.PlayerCrossbowExpertise },
        { PrestigeSystem.PrestigeType.GreatSwordExpertise, Core.DataStructures.PlayerGreatSwordExpertise },
        { PrestigeSystem.PrestigeType.SlashersExpertise, Core.DataStructures.PlayerSlashersExpertise },
        { PrestigeSystem.PrestigeType.PistolsExpertise, Core.DataStructures.PlayerPistolsExpertise },
        { PrestigeSystem.PrestigeType.ReaperExpertise, Core.DataStructures.PlayerReaperExpertise },
        { PrestigeSystem.PrestigeType.LongbowExpertise, Core.DataStructures.PlayerLongbowExpertise },
        { PrestigeSystem.PrestigeType.WhipExpertise, Core.DataStructures.PlayerWhipExpertise },
        { PrestigeSystem.PrestigeType.Sanguimancy, Core.DataStructures.PlayerSanguimancy },
        { PrestigeSystem.PrestigeType.WorkerLegacy, Core.DataStructures.PlayerWorkerLegacy },
        { PrestigeSystem.PrestigeType.WarriorLegacy, Core.DataStructures.PlayerWarriorLegacy },
        { PrestigeSystem.PrestigeType.ScholarLegacy, Core.DataStructures.PlayerScholarLegacy },
        { PrestigeSystem.PrestigeType.RogueLegacy, Core.DataStructures.PlayerRogueLegacy },
        { PrestigeSystem.PrestigeType.MutantLegacy, Core.DataStructures.PlayerMutantLegacy },
        { PrestigeSystem.PrestigeType.VBloodLegacy, Core.DataStructures.PlayerVBloodLegacy },
        { PrestigeSystem.PrestigeType.DraculinLegacy, Core.DataStructures.PlayerDraculinLegacy },
        { PrestigeSystem.PrestigeType.ImmortalLegacy, Core.DataStructures.PlayerImmortalLegacy },
        { PrestigeSystem.PrestigeType.CreatureLegacy, Core.DataStructures.PlayerCreatureLegacy },
        { PrestigeSystem.PrestigeType.BruteLegacy, Core.DataStructures.PlayerBruteLegacy }
    };
    public static Dictionary<PrestigeSystem.PrestigeType, Dictionary<ulong, KeyValuePair<int, float>>> PrestigeTypeToPlayerDataMap
    {
        get => prestigeTypeToPlayerDataMap;
    }

    static readonly Dictionary<PrestigeSystem.PrestigeType, int> prestigeTypeToMaxLevel = new()
    {
        { PrestigeSystem.PrestigeType.Experience, Plugin.MaxPlayerLevel.Value },
        { PrestigeSystem.PrestigeType.SwordExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.AxeExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.MaceExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.SpearExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.CrossbowExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.GreatSwordExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.SlashersExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.PistolsExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.ReaperExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.LongbowExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.WhipExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.Sanguimancy, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.WorkerLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.WarriorLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.ScholarLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.RogueLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.MutantLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.VBloodLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.DraculinLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.ImmortalLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.CreatureLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.BruteLegacy, Plugin.MaxBloodLevel.Value }
    };
    public static Dictionary<PrestigeSystem.PrestigeType, int> PrestigeTypeToMaxLevel
    {
       get => prestigeTypeToMaxLevel;
    }
    static readonly Dictionary<PrestigeSystem.PrestigeType, int> prestigeTypeToMaxPrestigeLevel = new()
    {
        { PrestigeSystem.PrestigeType.Experience, Plugin.MaxLevelingPrestiges.Value },
        { PrestigeSystem.PrestigeType.SwordExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.AxeExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.MaceExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.SpearExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.CrossbowExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.GreatSwordExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.SlashersExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.PistolsExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.ReaperExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.LongbowExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.WhipExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.Sanguimancy, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.WorkerLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.WarriorLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.ScholarLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.RogueLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.MutantLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.VBloodLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.DraculinLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.ImmortalLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.CreatureLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.BruteLegacy, Plugin.MaxLegacyPrestiges.Value }
    };
    public static Dictionary<PrestigeSystem.PrestigeType, int> PrestigeTypeToMaxPrestigeLevel
    {
        get => prestigeTypeToMaxPrestigeLevel;
    }
}
