using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using Stunlock.Core;

namespace Bloodcraft.Interfaces;
public enum ShapeshiftType
{
    EvolvedVampire,
    CorruptedSerpent
    // AncientGuardian
}
internal interface IShapeshift
{
    PrefabGUID ShapeshiftBuff { get; }
    List<PrefabGUID> AbilityGroups { get; }
    IReadOnlyDictionary<int, PrefabGUID> Abilities { get; }
    IReadOnlyDictionary<int, float> Cooldowns { get; }
    IReadOnlyDictionary<int, int> UnlockLevels { get; }
    PrefabGUID GetShapeshiftBuff();
    bool TryGetCooldown(PrefabGUID ability, out float cooldown);
}
internal abstract class Shapeshift : IShapeshift
{
    public abstract ShapeshiftType Type { get; }
    public abstract PrefabGUID ShapeshiftBuff { get; }
    public abstract List<PrefabGUID> AbilityGroups { get; }
    public abstract IReadOnlyDictionary<int, PrefabGUID> Abilities { get; }
    public abstract IReadOnlyDictionary<int, float> Cooldowns { get; }
    public virtual IReadOnlyDictionary<int, int> UnlockLevels => _emptyUnlockLevels;
    static readonly Dictionary<int, int> _emptyUnlockLevels = [];
    public virtual PrefabGUID GetShapeshiftBuff() => ShapeshiftBuff;
    public virtual bool TryGetCooldown(PrefabGUID ability, out float cooldown)
    {
        foreach (var (slot, guid) in Abilities)
        {
            if (guid.Equals(ability))
            {
                return Cooldowns.TryGetValue(slot, out cooldown);
            }
        }

        cooldown = 0f;
        return false;
    }
    public virtual bool TryGetSlot(PrefabGUID ability, out int slot)
    {
        foreach (var kvp in Abilities)
        {
            if (kvp.Value.Equals(ability))
            {
                slot = kvp.Key;
                return true;
            }
        }

        slot = -1;
        return false;
    }
}
internal class EvolvedVampire : Shapeshift
{
    public override ShapeshiftType Type => ShapeshiftType.EvolvedVampire;
    public override PrefabGUID ShapeshiftBuff => Buffs.EvolvedVampireBuff;
    public override List<PrefabGUID> AbilityGroups => _abilityGroups;
    public override IReadOnlyDictionary<int, PrefabGUID> Abilities => _abilities;
    public override IReadOnlyDictionary<int, float> Cooldowns => _cooldowns;

    static readonly List<PrefabGUID> _abilityGroups =
    [
        PrefabGUIDs.AB_Vampire_Dracula_ShockwaveFastSlash_AbilityGroup,
        PrefabGUIDs.AB_Vampire_Dracula_DownSwingDetonating_Abilitygroup,
        PrefabGUIDs.AB_Vampire_Dracula_QuickTeleport_AbilityGroup,
        PrefabGUIDs.AB_Vampire_Dracula_VeilOfBats_AbilityGroup,
        PrefabGUIDs.AB_Vampire_Dracula_SideStepLong_Followup_Abilitygroup,
        PrefabGUIDs.AB_Vampire_Dracula_EtherialSword_Abilitygroup,
        PrefabGUIDs.AB_Vampire_Dracula_RingOfBlood_AbilityGroup,
        PrefabGUIDs.AB_Blood_BloodStorm_AbilityGroup
    ];

    static readonly Dictionary<int, PrefabGUID> _abilities = new()
    {
        { 0, PrefabGUIDs.AB_Vampire_Dracula_ShockwaveFastSlash_AbilityGroup },
        { 1, PrefabGUIDs.AB_Vampire_Dracula_DownSwingDetonating_Abilitygroup },
        { 2, PrefabGUIDs.AB_Vampire_Dracula_QuickTeleport_AbilityGroup },
        { 3, PrefabGUIDs.AB_Vampire_Dracula_VeilOfBats_AbilityGroup },
        { 4, PrefabGUIDs.AB_Vampire_Dracula_SideStepLong_Followup_Abilitygroup },
        { 5, PrefabGUIDs.AB_Vampire_Dracula_EtherialSword_Abilitygroup },
        { 6, PrefabGUIDs.AB_Vampire_Dracula_RingOfBlood_AbilityGroup },
        { 7, PrefabGUIDs.AB_Blood_BloodStorm_AbilityGroup }
    };

    static readonly Dictionary<int, float> _cooldowns = new()
    {
        { 0, 0f },
        { 1, 8f },
        { 2, 0f },
        { 3, 8f },
        { 4, 8f },
        { 5, 15f },
        { 6, 25f },
        { 7, 35f }
    };
}
internal class CorruptedSerpent : Shapeshift
{
    public override ShapeshiftType Type => ShapeshiftType.CorruptedSerpent;
    public override PrefabGUID ShapeshiftBuff => Buffs.CorruptedSerpentBuff;
    public override List<PrefabGUID> AbilityGroups => _abilityGroups;
    public override IReadOnlyDictionary<int, PrefabGUID> Abilities => _abilities;
    public override IReadOnlyDictionary<int, float> Cooldowns => _cooldowns;

    static readonly List<PrefabGUID> _abilityGroups =
    [
        PrefabGUIDs.AB_Blackfang_Morgana_MeleeAttack_AbilityGroup,
        PrefabGUIDs.AB_Blackfang_Morgana_GroundPiercer_AbilityGroup,
        PrefabGUIDs.AB_Vampire_Dracula_QuickTeleport_AbilityGroup,
        PrefabGUIDs.AB_Blackfang_Morgana_MistSpinners_AbilityGroup,
        PrefabGUIDs.AB_Blackfang_Morgana_CrossWindSlash_AbilityGroup,
        PrefabGUIDs.AB_Blackfang_Morgana_SpectralBlast_AbilityGroup,
        PrefabGUIDs.AB_Blackfang_Morgana_SpectralBeam_AbilityGroup,
        PrefabGUIDs.AB_Blackfang_Morgana_EyeOfTheCorruption_Setup_AbilityGroup
    ];

    static readonly Dictionary<int, PrefabGUID> _abilities = new()
    {
        { 0, PrefabGUIDs.AB_Blackfang_Morgana_MeleeAttack_AbilityGroup },
        { 1, PrefabGUIDs.AB_Blackfang_Morgana_GroundPiercer_AbilityGroup },
        { 2, PrefabGUIDs.AB_Vampire_Dracula_QuickTeleport_AbilityGroup },
        { 3, PrefabGUIDs.AB_Blackfang_Morgana_MistSpinners_AbilityGroup },
        { 4, PrefabGUIDs.AB_Blackfang_Morgana_CrossWindSlash_AbilityGroup },
        { 5, PrefabGUIDs.AB_Blackfang_Morgana_SpectralBlast_AbilityGroup },
        { 6, PrefabGUIDs.AB_Blackfang_Morgana_SpectralBeam_AbilityGroup },
        { 7, PrefabGUIDs.AB_Blackfang_Morgana_EyeOfTheCorruption_Setup_AbilityGroup }
    };

    static readonly Dictionary<int, float> _cooldowns = new()
    {
        { 0, 0f },
        { 1, 8f },
        { 2, 0f },
        { 3, 20f },
        { 4, 8f },
        { 5, 15f },
        { 6, 25f },
        { 7, 35f }
    };
}

/*
internal class AncientGuardian : Shapeshift
{
    public override ShapeshiftType Type => ShapeshiftType.AncientGuardian;
    public override PrefabGUID ShapeshiftBuff => Buffs.AncientGuardianBuff;
    public override List<PrefabGUID> AbilityGroups => _abilityGroups;
    public override IReadOnlyDictionary<int, PrefabGUID> Abilities => _abilities;
    public override IReadOnlyDictionary<int, float> Cooldowns => _cooldowns;

    static readonly List<PrefabGUID> _abilityGroups =
    [
        PrefabGUIDs.AB_Geomancer_MeleeAttack_Group,
        PrefabGUIDs.AB_Geomancer_GroundSlam_Group,
        PrefabGUIDs.AB_Vampire_Dracula_QuickTeleport_AbilityGroup,
        PrefabGUIDs.AB_Geomancer_Golem_RaiseGuardians_AbilityGroup,
        PrefabGUIDs.AB_Geomancer_RockSlam_AbilityGroup,
        PrefabGUIDs.AB_Geomancer_Enrage_AbilityGroup,
        PrefabGUIDs.AB_Geomancer_EnragedSmash_AbilityGroup,
        PrefabGUIDs.AB_Geomancer_UndergroundTremmors_AbilityGroup
    ];

    static readonly Dictionary<int, PrefabGUID> _abilities = new()
    {
        { 0, PrefabGUIDs.AB_Geomancer_MeleeAttack_Group },
        { 1, PrefabGUIDs.AB_Geomancer_GroundSlam_Group },
        { 2, PrefabGUIDs.AB_Vampire_Dracula_QuickTeleport_AbilityGroup },
        { 3, PrefabGUIDs.AB_Geomancer_Golem_RaiseGuardians_AbilityGroup },
        { 4, PrefabGUIDs.AB_Geomancer_RockSlam_AbilityGroup },
        { 5, PrefabGUIDs.AB_Geomancer_Enrage_AbilityGroup },
        { 6, PrefabGUIDs.AB_Geomancer_EnragedSmash_AbilityGroup },
        { 7, PrefabGUIDs.AB_Geomancer_UndergroundTremmors_AbilityGroup }
    };

    static readonly Dictionary<int, float> _cooldowns = new()
    {
        { 0, 0f },
        { 1, 8f },
        { 2, 0f },
        { 3, 20f },
        { 4, 8f },
        { 5, 15f },
        { 6, 25f },
        { 7, 35f }
    };
}
*/
