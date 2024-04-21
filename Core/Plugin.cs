using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API;
using Cobalt.Hooks;
using HarmonyLib;
using ProjectM;
using System.Reflection;
using Unity.Entities;
using VampireCommandFramework;
using static Cobalt.Systems.Weapon.WeaponStatsSystem.WeaponStatManager.WeaponFocusSystem;
using WeaponStatType = Cobalt.Systems.Weapon.WeaponStatsSystem.WeaponStatManager.WeaponFocusSystem.WeaponStatType;

namespace Cobalt.Core
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gg.deca.Bloodstone")]
    [BepInDependency("gg.deca.VampireCommandFramework")]
    public class Plugin : BasePlugin, IRunOnInitialized
    {
        private Harmony _harmony;
        internal static Plugin Instance { get; private set; }

        private static ManualLogSource Logger;
        public new static ManualLogSource Log => Logger;

        public static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
        public static readonly string PlayerExperienceJson = Path.Combine(Plugin.ConfigPath, "player_experience.json");
        public static readonly string PlayerMasteryJson = Path.Combine(Plugin.ConfigPath, "player_mastery.json");
        public static readonly string PlayerBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_bloodline.json");
        public static readonly string PlayerBoolsJson = Path.Combine(Plugin.ConfigPath, "player_bools.json");
        public static readonly string PlayerWeaponStatsJson = Path.Combine(Plugin.ConfigPath, "player_weapon_stats.json");
        public static readonly string PlayerBloodlineStatsJson = Path.Combine(Plugin.ConfigPath, "player_bloodline_stats.json");

        public override void Load()
        {
            Instance = this;
            Logger = base.Log;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            CommandRegistry.RegisterAll();
            InitConfig();
            ServerEventsPatch.OnGameDataInitialized += GameDataOnInitialize;
            LoadAllData();
            //UpdateStats();
            Plugin.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");
        }

        private void GameDataOnInitialize(World world)
        {
        }

        private static void UpdateStats()
        {
            VampireStatModifiers vampireStatModifiers = VWorld.Server.GetExistingSystem<ServerGameSettingsSystem>()._Settings.VampireStatModifiers;
            UpdateStatCaps(vampireStatModifiers);
        }

        private static void UpdateStatCaps(VampireStatModifiers vampireStatModifiers)
        {
            WeaponStatCaps.BaseCaps[WeaponStatType.MaxHealth] *= vampireStatModifiers.MaxHealthModifier;
            WeaponStatCaps.BaseCaps[WeaponStatType.PhysicalPower] *= vampireStatModifiers.PhysicalPowerModifier;
            WeaponStatCaps.BaseCaps[WeaponStatType.SpellPower] *= vampireStatModifiers.SpellPowerModifier;
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
            SaveAllData();  // Call a method that saves all data types.
            return true;
        }

        private static void SaveAllData()
        {
            DataStructures.SavePlayerExperience();
            DataStructures.SavePlayerMastery();
            DataStructures.SavePlayerBloodLine();
            DataStructures.SavePlayerBools();
            DataStructures.SavePlayerWeaponStats();
            DataStructures.SavePlayerBloodlineStats();
        }

        private static void LoadAllData()
        {
            DataStructures.LoadPlayerExperience();
            DataStructures.LoadPlayerMastery();
            DataStructures.LoadPlayerBloodLine();
            DataStructures.LoadPlayerBools();
            DataStructures.LoadPlayerWeaponStats();
            DataStructures.LoadPlayerBloodlineStats();
        }

        public void OnGameInitialized()
        {
        }
    }
}