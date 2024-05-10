using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Cobalt.Hooks;
using HarmonyLib;
using ProjectM;
using System.Reflection;
using Unity.Entities;
using VampireCommandFramework;
using static Cobalt.Systems.Expertise.WeaponStatsSystem.WeaponStatManager;
using static Cobalt.Hooks.BaseStats;
using ProjectM.CastleBuilding;
using Il2CppInterop.Runtime;
using Unity.Collections;
using Stunlock.Core;
using ProjectM.UI;

namespace Cobalt.Core
{
    [BepInPlugin(Cobalt.MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    //[BepInDependency("gg.deca.Bloodstone")]
    //[BepInDependency("gg.deca.VampireCommandFramework")]
    public class Plugin : BasePlugin
    {
        private Harmony _harmony;
        internal static Plugin Instance { get; private set; }

        private static ManualLogSource Logger;
        public new static ManualLogSource Log => Logger;

        public static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
        public static readonly string PlayerExperienceJson = Path.Combine(Plugin.ConfigPath, "player_experience.json");
        public static readonly string PlayerPrestigeJson = Path.Combine(Plugin.ConfigPath, "player_prestige.json");
        public static readonly string PlayerBoolsJson = Path.Combine(Plugin.ConfigPath, "player_bools.json");
        public static readonly string PlayerWoodcuttingJson = Path.Combine(Plugin.ConfigPath, "player_woodcutting.json");
        public static readonly string PlayerMiningJson = Path.Combine(Plugin.ConfigPath, "player_mining.json");
        public static readonly string PlayerFishingJson = Path.Combine(Plugin.ConfigPath, "player_fishing.json");
        public static readonly string PlayerBlacksmithingJson = Path.Combine(Plugin.ConfigPath, "player_blacksmithing.json");
        public static readonly string PlayerTailoringJson = Path.Combine(Plugin.ConfigPath, "player_tailoring.json");
        public static readonly string PlayerJewelcraftingJson = Path.Combine(Plugin.ConfigPath, "player_jewelcrafting.json");
        public static readonly string PlayerAlchemyJson = Path.Combine(Plugin.ConfigPath, "player_alchemy.json");
        public static readonly string PlayerHarvestingJson = Path.Combine(Plugin.ConfigPath, "player_harvesting.json");
        public static readonly string PlayerSwordMasteryJson = Path.Combine(Plugin.ConfigPath, "player_sword.json");
        public static readonly string PlayerAxeMasteryJson = Path.Combine(Plugin.ConfigPath, "player_axe.json");
        public static readonly string PlayerMaceMasteryJson = Path.Combine(Plugin.ConfigPath, "player_mace.json");
        public static readonly string PlayerSpearMasteryJson = Path.Combine(Plugin.ConfigPath, "player_spear.json");
        public static readonly string PlayerCrossbowMasteryJson = Path.Combine(Plugin.ConfigPath, "player_crossbow.json");
        public static readonly string PlayerGreatSwordMastery = Path.Combine(Plugin.ConfigPath, "player_greatsword.json");
        public static readonly string PlayerSlashersMasteryJson = Path.Combine(Plugin.ConfigPath, "player_slashers.json");
        public static readonly string PlayerPistolsMasteryJson = Path.Combine(Plugin.ConfigPath, "player_pistols.json");
        public static readonly string PlayerReaperMastery = Path.Combine(Plugin.ConfigPath, "player_reaper.json");
        public static readonly string PlayerLongbowMasteryJson = Path.Combine(Plugin.ConfigPath, "player_longbow.json");
        public static readonly string PlayerUnarmedMasteryJson = Path.Combine(Plugin.ConfigPath, "player_unarmed.json");
        public static readonly string PlayerWhipMasteryJson = Path.Combine(Plugin.ConfigPath, "player_whip.json");
        public static readonly string PlayerSanguimancyJson = Path.Combine(Plugin.ConfigPath, "player_sanguimancy.json");

        //public static readonly string PlayerWeaponStatsJson = Path.Combine(Plugin.ConfigPath, "player_weapon_stats.json");
        public static readonly string PlayerEquippedWeaponJson = Path.Combine(Plugin.ConfigPath, "player_equipped_weapon.json");

        public static readonly string PlayerWeaponChoicesJson = Path.Combine(Plugin.ConfigPath, "player_weapon_choices.json");
        public static readonly string PlayerBloodChoicesJson = Path.Combine(Plugin.ConfigPath, "player_blood_choices.json");

        public override void Load()
        {
            Instance = this;
            Logger = base.Log;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            InitConfig();
            CommandRegistry.RegisterAll();
            LoadAllData();
            //UpdateStats();
            Plugin.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");

        }

        
        private void GameDataOnInitialize(World world)
        {
            // entity modifications go here?

        }

        private static void InitConfig()
        {
            // Initialize configuration settings
            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
            }
        }

        public override bool Unload()
        {
            Config.Clear();
            _harmony.UnpatchSelf();
            //SaveAllData();  // Call a method that saves all data types.
            return true;
        }

        public static void SaveAllData()
        {
            foreach (var saveFunction in saveFunctions)
            {
                saveFunction();
            }
        }

        private static void LoadAllData()
        {
            foreach (var loadFunction in loadFunctions)
            {
                loadFunction();
            }
        }

        private static readonly Action[] saveFunctions =
        [
            DataStructures.SavePlayerExperience,
            DataStructures.SavePlayerPrestige,
            DataStructures.SavePlayerBools,
            DataStructures.SavePlayerWoodcutting,
            DataStructures.SavePlayerMining,
            DataStructures.SavePlayerFishing,
            DataStructures.SavePlayerBlacksmithing,
            DataStructures.SavePlayerTailoring,
            DataStructures.SavePlayerJewelcrafting,
            DataStructures.SavePlayerAlchemy,
            DataStructures.SavePlayerHarvesting,
            DataStructures.SavePlayerSwordMastery,
            DataStructures.SavePlayerAxeMastery,
            DataStructures.SavePlayerMaceMastery,
            DataStructures.SavePlayerSpearMastery,
            DataStructures.SavePlayerCrossbowMastery,
            DataStructures.SavePlayerGreatSwordMastery,
            DataStructures.SavePlayerSlashersMastery,
            DataStructures.SavePlayerPistolsMastery,
            DataStructures.SavePlayerReaperMastery,
            DataStructures.SavePlayerLongbowMastery,
            DataStructures.SavePlayerWhipMastery,
            DataStructures.SavePlayerUnarmedMastery,
            DataStructures.SavePlayerSanguimancy,
            DataStructures.SavePlayerWeaponChoices,
            DataStructures.SavePlayerEquippedWeapon,
            DataStructures.SavePlayerBloodChoices
        ];

        private static readonly Action[] loadFunctions =
        [
            DataStructures.LoadPlayerExperience,
            DataStructures.LoadPlayerPrestige,
            DataStructures.LoadPlayerBools,
            DataStructures.LoadPlayerWoodcutting,
            DataStructures.LoadPlayerMining,
            DataStructures.LoadPlayerFishing,
            DataStructures.LoadPlayerBlacksmithing,
            DataStructures.LoadPlayerTailoring,
            DataStructures.LoadPlayerJewelcrafting,
            DataStructures.LoadPlayerAlchemy,
            DataStructures.LoadPlayerHarvesting,
            DataStructures.LoadPlayerSanguimancy,
            DataStructures.LoadPlayerSwordMastery,
            DataStructures.LoadPlayerAxeMastery,
            DataStructures.LoadPlayerMaceMastery,
            DataStructures.LoadPlayerSpearMastery,
            DataStructures.LoadPlayerCrossbowMastery,
            DataStructures.LoadPlayerGreatSwordMastery,
            DataStructures.LoadPlayerSlashersMastery,
            DataStructures.LoadPlayerPistolsMastery,
            DataStructures.LoadPlayerReaperMastery,
            DataStructures.LoadPlayerLongbowMastery,
            DataStructures.LoadPlayerWhipMastery,
            DataStructures.LoadPlayerUnarmedMastery,
            DataStructures.LoadPlayerWeaponChoices,
            DataStructures.LoadPlayerEquippedWeapon,
            DataStructures.LoadPlayerBloodStats
        ];

        public static void OnGameInitialized()
        {
            
        }

        public static void UpdateBaseStats()
        {
            VampireStatModifiers vampireStatModifiers = VWorld.Server.GetExistingSystemManaged<ServerGameSettingsSystem>()._Settings.VampireStatModifiers;
            BaseWeaponStats[WeaponStatType.PhysicalPower] *= vampireStatModifiers.PhysicalPowerModifier;
            BaseWeaponStats[WeaponStatType.SpellPower] *= vampireStatModifiers.SpellPowerModifier;
            
        }

        public static void StripLevelSources()
        {
            EntityQuery equippablesQuery = VWorld.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] {
                    ComponentType.ReadOnly<Equippable>(),
                },
                Options = EntityQueryOptions.IncludeAll
            });
            NativeArray<Entity> equippables = equippablesQuery.ToEntityArray(Allocator.TempJob);
            PrefabCollectionSystem prefabCollectionSystem = VWorld.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
            try
            {
                foreach (var equippable in equippables)
                {
                    if (!equippable.Has<PrefabGUID>()) continue;
                    Plugin.Log.LogInfo($"Removing level source from {equippable.Read<PrefabGUID>().LookupName()}...");
                    PrefabGUID equippablePrefab = equippable.Read<PrefabGUID>();
                    Entity entity = prefabCollectionSystem._PrefabGuidToEntityMap[equippablePrefab];
                    if (entity.Has<ArmorLevelSource>())
                    {
                        ArmorLevelSource armorLevelSource = entity.Read<ArmorLevelSource>();
                        armorLevelSource.Level = 0f;
                        entity.Write(armorLevelSource);
                    }
                    else if (entity.Has<SpellLevelSource>())
                    {
                        SpellLevelSource spellLevelSource = entity.Read<SpellLevelSource>();
                        spellLevelSource.Level = 0f;
                        entity.Write(spellLevelSource);
                    }
                    else if (entity.Has<WeaponLevelSource>())
                    {
                        WeaponLevelSource weaponLevelSource = entity.Read<WeaponLevelSource>();
                        weaponLevelSource.Level = 0f;
                        entity.Write(weaponLevelSource);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
            finally
            {
                equippablesQuery.Dispose();
                equippables.Dispose();
            }

        }

        

        
    }
}