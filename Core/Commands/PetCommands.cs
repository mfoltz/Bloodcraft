using Bloodstone.API;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using System.Text.RegularExpressions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using VCreate.Core.Toolbox;
using VCreate.Hooks;
using VCreate.Systems;
using VRising.GameData.Models;
using VRising.GameData.Utils;
using static VCreate.Core.Toolbox.FontColors;
using static VCreate.Hooks.PetSystem.UnitTokenSystem;

namespace VCreate.Core.Commands
{
    public class PetCommands
    {
        internal static Dictionary<ulong, FamiliarStasisState> PlayerFamiliarStasisMap = [];



        [Command(name: "setUnlocked", shortHand: "set", adminOnly: false, usage: ".set [#]", description: "Sets familiar to attempt binding to from unlocked units.")]
        public static void MethodMinusOne(ChatCommandContext ctx, int choice)
        {
            ulong platformId = ctx.User.PlatformId;
            if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out var map))
            {
                var profiles = map.Values;

                foreach (var profile in profiles)
                {
                    if (profile.Active)
                    {
                        ctx.Reply("You have an active familiar. Unbind it before setting another.");
                        return;
                    }
                }
            }
            if (PlayerFamiliarStasisMap.TryGetValue(platformId, out var familiarStasisState) && familiarStasisState.IsInStasis)
            {
                ctx.Reply("You have a familiar in stasis. If you want to set another to bind, call it and unbind first.");
                return;
            }
            if (DataStructures.UnlockedPets.TryGetValue(platformId, out var data))
            {
                if (choice < 1 || choice > data.Count)
                {
                    ctx.Reply($"Invalid choice, please use 1 to {data.Count}.");
                    return;
                }
                if (DataStructures.PlayerSettings.TryGetValue(platformId, out var settings))
                {
                    settings.Familiar = data[choice - 1];
                    DataStructures.PlayerSettings[platformId] = settings;
                    DataStructures.SavePlayerSettings();
                    PrefabGUID prefabGUID = new(data[choice - 1]);
                    string colorfam = VCreate.Core.Toolbox.FontColors.Pink(prefabGUID.LookupName());
                    ctx.Reply($"Familiar to attempt binding to set: {colorfam}");
                }
                else
                {
                    ctx.Reply("Couldn't find data to set unlocked.");
                    return;
                }
            }
            else
            {
                ctx.Reply("You don't have any unlocked familiars yet.");
            }
        }

        [Command(name: "removeUnlocked", shortHand: "remove", adminOnly: false, usage: ".remove [#]", description: "Removes choice from list of unlocked familiars to bind to.")]
        public static void RemoveUnlocked(ChatCommandContext ctx, int choice)
        {
            ulong platformId = ctx.User.PlatformId;
            if (DataStructures.UnlockedPets.TryGetValue(platformId, out var data))
            {
                if (choice < 1 || choice > data.Count)
                {
                    ctx.Reply($"Invalid choice, please use 1 to {data.Count} for removing.");
                    return;
                }
                if (DataStructures.PlayerSettings.TryGetValue(platformId, out var settings))
                {
                    var toRemove = data[choice - 1];
                    if (data.Contains(toRemove))
                    {
                        data.Remove(toRemove);
                        DataStructures.UnlockedPets[platformId] = data;
                        DataStructures.SaveUnlockedPets();

                        ctx.Reply($"Familiar removed from list of unlocked units.");
                    }
                    else
                    {
                        ctx.Reply("Failed to remove unlocked unit.");
                        return;
                    }
                }
                else
                {
                    ctx.Reply("Couldn't find data to remove unlocked unit.");
                    return;
                }
            }
            else
            {
                ctx.Reply("You don't have any unlocked familiars yet.");
            }
        }

        [Command(name: "resetFamiliarProfile", shortHand: "rfp", adminOnly: false, usage: ".rfp [#]", description: "Resets familiar profile, allowing it to level again.")]
        public static void ResetFam(ChatCommandContext ctx, int choice)
        {
            ulong platformId = ctx.User.PlatformId;
            if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out var data))
            {
                if (DataStructures.UnlockedPets.TryGetValue(platformId, out var unlocked))
                {
                    if (choice < 1 || choice > unlocked.Count)
                    {
                        ctx.Reply($"Invalid choice, please use 1 to {unlocked.Count}.");
                        return;
                    }
                    Entity familiar = FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);
                    if (familiar.Equals(Entity.Null))
                    {
                        ctx.Reply("Toggle your familiar before resetting it.");
                        return;
                    }
                    else
                    {
                        if (data.TryGetValue(familiar.Read<PrefabGUID>().LookupName().ToString(), out PetExperienceProfile profile) && profile.Active)
                        {
                            profile.Level = 0;
                            profile.Stats.Clear();
                            profile.Active = false;

                            data[familiar.Read<PrefabGUID>().LookupName().ToString()] = profile;
                            DataStructures.PlayerPetsMap[platformId] = data;
                            DataStructures.SavePetExperience();
                            SystemPatchUtil.Destroy(familiar);
                            ctx.Reply("Profile reset, familiar unbound.");
                        }
                        else
                        {
                            ctx.Reply("Couldn't find active familiar in followers to reset.");
                        }
                    }
                }
                else
                {
                    ctx.Reply("You don't have any unlocked familiars yet.");
                }
            }
            else
            {
                ctx.Reply("You don't have any familiars.");
                return;
            }
        }

        [Command(name: "listFamiliars", shortHand: "listfam", adminOnly: false, usage: ".listfam", description: "Lists unlocked familiars.")]
        public static void MethodZero(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;
            if (DataStructures.UnlockedPets.TryGetValue(platformId, out var data))
            {
                if (data.Count == 0)
                {
                    ctx.Reply("You don't have any unlocked familiars yet.");
                    return;
                }
                int counter = 0;
                foreach (var unlock in data)
                {
                    counter++;
                    string colornum = VCreate.Core.Toolbox.FontColors.Green(counter.ToString());
                    PrefabGUID prefabGUID = new(unlock);
                    // want real name from guid
                    string colorfam = VCreate.Core.Toolbox.FontColors.Pink(prefabGUID.LookupName());
                    ctx.Reply($"{colornum}: {colorfam}");
                }
            }
            else
            {
                ctx.Reply("You don't have any unlocked familiars yet.");
                return;
            }
        }

        [Command(name: "bindFamiliar", shortHand: "bind", adminOnly: false, usage: ".bind", description: "Binds familiar with correct gem in inventory.")]
        public static void MethodOne(ChatCommandContext ctx)
        {
            ServerGameManager serverGameManager = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
            EntityManager entityManager = VWorld.Server.EntityManager;
            ulong platformId = ctx.User.PlatformId;

            // verify states before proceeding, make sure no active profiles and no familiars in stasis
            Entity character = ctx.Event.SenderCharacterEntity;

            if (!FollowerSystemPatchV2.StateCheckUtility.ValidatePlayerState(character, platformId, entityManager))
            {
                ctx.Reply("You can't bind to a familiar while shapeshifted or dominating presence is active.");
                return;
            }


            var followers = character.ReadBuffer<FollowerBuffer>();
            foreach (var follower in followers)
            {
                var buffs = follower.Entity._Entity.ReadBuffer<BuffBuffer>();
                foreach (var buff in buffs)
                {
                    if (buff.PrefabGuid.GuidHash == VCreate.Data.Prefabs.AB_Charm_Active_Human_Buff.GuidHash)
                    {
                        ctx.Reply("Looks like you have a charmed human. Take care of that before binding to a familiar.");
                        return;
                    }
                }
            }

            if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out var data))
            {
                var profiles = data.Values;

                foreach (var profile in profiles)
                {
                    if (profile.Active)
                    {
                        ctx.Reply("You already have an active familiar profile. Unbind it before binding to another.");
                        return;
                    }
                }
            }
            if (PlayerFamiliarStasisMap.TryGetValue(platformId, out var familiarStasisState) && familiarStasisState.IsInStasis)
            {
                ctx.Reply("You have a familiar in stasis. If you want to bind to another, call it and unbind first.");
                return;
            }
            bool flag = false;
            
            if (DataStructures.PlayerSettings.TryGetValue(platformId, out var settings))
            {
                UserModel userModel = VRising.GameData.GameData.Users.GetUserByPlatformId(platformId);
                var inventory = userModel.Inventory.Items;
                if (settings.Familiar == 0)
                {
                    ctx.Reply("You haven't set a familiar to bind. Use .set [#] to select an unlocked familiar from .listfam");
                    return;
                }
                if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out var map))
                {
                    int cost = 250;
                    PrefabGUID key = new(settings.Familiar);
                    string newKey = key.LookupName();
                    if (map.TryGetValue(newKey, out PetExperienceProfile profile) && profile.Unlocked) // if profile already exists and is unlocked charge blood essence
                    {
                        // take blood essence to rebind
                        if (serverGameManager.TryRemoveInventoryItem(ctx.Event.SenderCharacterEntity, VCreate.Data.Prefabs.Item_BloodEssence_T01, cost))
                        {
                            
                            settings.Binding = true;
                            OnHover.SummonFamiliar(ctx.Event.SenderCharacterEntity.Read<PlayerCharacter>().UserEntity, new(settings.Familiar));
                            
                        }
                        else
                        {
                            ctx.Reply($"You don't have enough <color=red>blood essence</color> to revive your familiar. (<color=white>{cost}</color>)");
                            return;
                        }
                        return;
                    }
                    
                }
                Entity unlocked = VWorld.Server.GetExistingSystem<PrefabCollectionSystem>()._PrefabGuidToEntityMap[new(settings.Familiar)];
                EntityCategory unitCategory = unlocked.Read<EntityCategory>();
                //Plugin.Log.LogInfo(unitCategory.UnitCategory.ToString());
                PrefabGUID gem;
                if (unlocked.Read<PrefabGUID>().LookupName().ToLower().Contains("vblood"))
                {
                    gem = new(PetSystem.UnitTokenSystem.UnitToGemMapping.UnitCategoryToGemPrefab[UnitToGemMapping.UnitType.VBlood]);
                }
                else
                {
                    gem = new(PetSystem.UnitTokenSystem.UnitToGemMapping.UnitCategoryToGemPrefab[(UnitToGemMapping.UnitType)unitCategory.UnitCategory]);
                }
                
                
                foreach (var item in inventory)
                {
                    if (item.Item.PrefabGUID.GuidHash == gem.GuidHash)
                    {
                        flag = serverGameManager.TryRemoveInventoryItem(ctx.Event.SenderCharacterEntity, gem, 1);
                        //flag = InventoryUtilitiesServer.TryRemoveItemFromInventories(VWorld.Server.EntityManager, ctx.Event.SenderCharacterEntity, gem, 1);
                        /*
                        if (!flag && character.Has<BagHolder>())
                        {
                            // if not found in main check gembag
                            BagHolder bagHolder = character.Read<BagHolder>();
                            BagInstance bagInstance1 = bagHolder.BagInstance0;
                            BagInstance bagInstance2 = bagHolder.BagInstance1;
                            BagInstance bagInstance3 = bagHolder.BagInstance2;
                            BagInstance bagInstance4 = bagHolder.BagInstance3;
                            if (!bagInstance1.Entity._Entity.Equals(Entity.Null))
                            {
                                flag = serverGameManager.TryRemoveInventoryItem(bagInstance1.Entity._Entity, gem, 1);
                                if (flag) break;
                                else
                                {
                                    if (!bagInstance2.Entity._Entity.Equals(Entity.Null))
                                    {
                                        flag = serverGameManager.TryRemoveInventoryItem(bagInstance2.Entity._Entity, gem, 1);
                                        if (flag) break;
                                        else
                                        {
                                            if (!bagInstance3.Entity._Entity.Equals(Entity.Null))
                                            {
                                                flag = serverGameManager.TryRemoveInventoryItem(bagInstance3.Entity._Entity, gem, 1);
                                                if (flag) break;
                                                else
                                                {
                                                    if (!bagInstance4.Entity._Entity.Equals(Entity.Null))
                                                    {
                                                        flag = serverGameManager.TryRemoveInventoryItem(bagInstance4.Entity._Entity, gem, 1);
                                                        if (flag) break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            

                        }
                        */
                        
                        break;
                    }
                }
                if (flag)
                {
                    if (DataStructures.PlayerSettings.TryGetValue(platformId, out var Settings))
                    {
                        Settings.Binding = true;
                        OnHover.SummonFamiliar(ctx.Event.SenderCharacterEntity.Read<PlayerCharacter>().UserEntity, new(settings.Familiar));
                    }
                }
                else
                {
                    string colorString = FontColors.White(gem.GetPrefabName());
                    ctx.Reply($"Couldn't find flawless gem to unlock familiar type, make sure it's in your main inventory: ({colorString})");
                }
            }
            else
            {
                ctx.Reply("Couldn't find data to bind familiar.");
                return;
            }

            // check for correct gem to take away for binding to familiar
        }

        [Command(name: "unbindFamiliar", shortHand: "unbind", adminOnly: false, usage: ".unbind", description: "Deactivates familiar profile and lets you bind to a different familiar.")]
        public static void MethodTwo(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;

            if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out Dictionary<string, PetExperienceProfile> data))
            {
                if (PlayerFamiliarStasisMap.TryGetValue(platformId, out FamiliarStasisState familiarStasisState) && familiarStasisState.IsInStasis)
                {
                    ctx.Reply("You have a familiar in stasis. Call it before unbinding.");
                    return;
                }

                Entity familiar = FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);
                if (!familiar.Equals(Entity.Null) && data.TryGetValue(familiar.Read<PrefabGUID>().LookupName().ToString(), out PetExperienceProfile profile) && profile.Active)
                {
                    UnitStats stats = familiar.Read<UnitStats>();
                    Health health = familiar.Read<Health>();
                    float maxhealth = health.MaxHealth._Value;
                    float attackspeed = stats.AttackSpeed._Value;
                    float primaryattackspeed = stats.PrimaryAttackSpeed._Value;
                    float physicalpower = stats.PhysicalPower._Value;
                    float spellpower = stats.SpellPower._Value;
                    float physcritchance = stats.PhysicalCriticalStrikeChance._Value;
                    float physcritdamage = stats.PhysicalCriticalStrikeDamage._Value;
                    float spellcritchance = stats.SpellCriticalStrikeChance._Value;
                    float spellcritdamage = stats.SpellCriticalStrikeDamage._Value;
                    profile.Stats.Clear();
                    profile.Stats.AddRange([maxhealth, attackspeed, primaryattackspeed, physicalpower, spellpower, physcritchance, physcritdamage, spellcritchance, spellcritdamage]);
                    profile.Active = false;
                    profile.Combat = true;
                    data[familiar.Read<PrefabGUID>().LookupName().ToString()] = profile;
                    DataStructures.PlayerPetsMap[platformId] = data;
                    DataStructures.SavePetExperience();
                    SystemPatchUtil.Destroy(familiar);
                    ctx.Reply("Familiar profile deactivated, stats saved and familiar unbound. You may now bind to another.");
                }
                else if (familiar.Equals(Entity.Null))
                {
                    var profiles = data.Keys;
                    foreach (var key in profiles)
                    {
                        if (data[key].Active)
                        {
                            // remember if code gets here it means familiar also not in stasis so probably has been killed, unbind it
                            data.TryGetValue(key, out PetExperienceProfile dataprofile);
                            dataprofile.Active = false;
                            data[key] = dataprofile;
                            DataStructures.PlayerPetsMap[platformId] = data;
                            DataStructures.SavePetExperience();
                            ctx.Reply("Unable to locate familiar and not in stasis, assuming dead and unbinding.");
                            return;
                        }
                    }
                    ctx.Reply("Couldn't find active familiar in followers.");
                }
                else
                {
                    ctx.Reply("You don't have an active familiar to unbind.");
                }
            }
            else
            {
                ctx.Reply("You don't have a familiar to unbind.");
                return;
            }
        }

        /*
        //[Command(name: "enableFamiliar", shortHand: "call", usage: ".call", description: "Summons familar if found in stasis.", adminOnly: false)]
        public static void EnableFamiliar(ChatCommandContext ctx)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            ulong platformId = ctx.User.PlatformId;
            if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out Dictionary<string, PetExperienceProfile> data))
            {
                if (PlayerFamiliarStasisMap.TryGetValue(platformId, out FamiliarStasisState familiarStasisState) && familiarStasisState.IsInStasis)
                {
                    if (entityManager.Exists(familiarStasisState.FamiliarEntity))
                    {
                        SystemPatchUtil.Enable(familiarStasisState.FamiliarEntity);

                        Follower follower = familiarStasisState.FamiliarEntity.Read<Follower>();
                        follower.Followed._Value = ctx.Event.SenderCharacterEntity;
                        familiarStasisState.FamiliarEntity.Write(follower);
                        familiarStasisState.FamiliarEntity.Write(new Translation { Value = ctx.Event.SenderCharacterEntity.Read<Translation>().Value });
                        familiarStasisState.FamiliarEntity.Write(new LastTranslation { Value = ctx.Event.SenderCharacterEntity.Read<Translation>().Value });
                        familiarStasisState.IsInStasis = false;
                        familiarStasisState.FamiliarEntity = Entity.Null;
                        PlayerFamiliarStasisMap[platformId] = familiarStasisState;
                        ctx.Reply("Your familiar has been summoned.");
                    }
                    else
                    {
                        familiarStasisState.IsInStasis = false;
                        familiarStasisState.FamiliarEntity = Entity.Null;
                        PlayerFamiliarStasisMap[platformId] = familiarStasisState;
                        ctx.Reply("Familiar entity in stasis couldn't be found to enable, you may now unbind.");
                    }
                }
                else
                {
                    ctx.Reply("No familiars in stasis to enable.");
                }
            }
            else
            {
                ctx.Reply("No familiars found.");
            }
        }

        //[Command(name: "disableFamiliar", shortHand: "dismiss", adminOnly: false, usage: ".dismiss", description: "Puts summoned familiar in stasis.")]
        public static void MethodThree(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;
            if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out Dictionary<string, PetExperienceProfile> data))
            {
                Entity familiar = FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);
                if (familiar.Equals(Entity.Null) || !familiar.Has<PrefabGUID>())
                {
                    ctx.Reply("You don't have any familiars to disable.");
                }
                if (data.TryGetValue(familiar.Read<PrefabGUID>().LookupName().ToString(), out PetExperienceProfile profile) && profile.Active)
                {
                    Follower follower = familiar.Read<Follower>();
                    follower.Followed._Value = Entity.Null;
                    familiar.Write(follower);
                    SystemPatchUtil.Disable(familiar);
                    PlayerFamiliarStasisMap[platformId] = new FamiliarStasisState(familiar, true);
                    ctx.Reply("Your familiar has been put in stasis.");
                    //DataStructures.SavePetExperience();
                }
                else
                {
                    ctx.Reply("You don't have an active familiar to disable.");
                }
            }
            else
            {
                ctx.Reply("You don't have any familiars to disable.");
                return;
            }
        }
        */

        [Command(name: "setFamiliarFocus", shortHand: "focus", adminOnly: false, usage: ".focus [#]", description: "Sets the stat your familiar will specialize in when leveling up.")]
        public static void MethodFour(ChatCommandContext ctx, int stat)
        {
            ulong platformId = ctx.User.PlatformId;
            if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out Dictionary<string, PetExperienceProfile> data))
            {
                Entity familiar = FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);
                if (data.TryGetValue(familiar.Read<PrefabGUID>().LookupName().ToString(), out PetExperienceProfile profile) && profile.Active)
                {
                    int toSet = stat - 1;
                    if (toSet < 0 || toSet > PetSystem.PetFocusSystem.FocusToStatMap.FocusStatMap.Count - 1)
                    {
                        ctx.Reply($"Invalid choice, please use 1 to {PetSystem.PetFocusSystem.FocusToStatMap.FocusStatMap.Count}. Use .stats to see options.");
                        return;
                    }
                    profile.Focus = toSet;
                    data[familiar.Read<PrefabGUID>().LookupName().ToString()] = profile;

                    DataStructures.SavePetExperience();
                    ctx.Reply($"Familiar focus set to {PetSystem.PetFocusSystem.FocusToStatMap.FocusStatMap[toSet]}.");
                    return;
                }
                else
                {
                    ctx.Reply("Couldn't find active familiar in followers.");
                }
            }
        }

        [Command(name: "chooseMaxBuff", shortHand: "max", adminOnly: false, usage: ".max [#]", description: "Chooses buff for familiar to receieve when binding if at level 80.")]
        public static void ChooseMaxBuff(ChatCommandContext ctx, int choice)
        {
            ulong platformId = ctx.User.PlatformId;
            var buffs = VCreate.Hooks.PetSystem.DeathEventHandlers.BuffChoiceToNameMap;
            if (choice < 1 || choice > buffs.Count)
            {
                ctx.Reply($"Invalid choice, please use 1 to {buffs.Count}.");
                return;
            }
            var toSet = buffs[choice];
            var map = VCreate.Hooks.PetSystem.DeathEventHandlers.BuffNameToGuidMap;
            if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out Dictionary<string, PetExperienceProfile> data))
            {
                Entity familiar = FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);
                if (familiar.Equals(Entity.Null))
                {
                    ctx.Reply("Make sure your familiar is present before setting this.");
                    return;
                }
                if (data.TryGetValue(familiar.Read<PrefabGUID>().LookupName().ToString(), out PetExperienceProfile profile) && profile.Active)
                {
                    if (DataStructures.PetBuffMap[platformId].TryGetValue(familiar.Read<PrefabGUID>().GuidHash, out var buffData))
                    {
                        if (buffData.ContainsKey("Buffs"))
                        {
                            buffData["Buffs"].Clear();
                            buffData["Buffs"].Add(map[toSet]);
                            DataStructures.SavePetBuffMap();
                            ctx.Reply($"Max buff set to {toSet}.");
                        }
                        else
                        {
                            HashSet<int> newInts = [];
                            newInts.Add(map[toSet]);
                            buffData.Add("Buffs", newInts);
                            DataStructures.SavePetBuffMap();
                            ctx.Reply($"Max buff set to {toSet}.");
                        }
                    }
                    else
                    {
                        Dictionary<string, HashSet<int>> newDict = [];
                        HashSet<int> newInts = [];
                        newInts.Add(map[toSet]);
                        newDict.Add("Buffs", newInts);
                        DataStructures.PetBuffMap[platformId].Add(familiar.Read<PrefabGUID>().GuidHash, newDict);
                        DataStructures.SavePetBuffMap();
                        ctx.Reply($"Max buff set to {toSet}.");
                    }
                }
                else
                {
                    ctx.Reply("Couldn't find active familiar in followers.");
                }
            }
        }

        [Command(name: "listStats", shortHand: "stats", adminOnly: false, usage: ".stats", description: "Lists stats of active familiar.")]
        public static void ListFamStats(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;
            if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out Dictionary<string, PetExperienceProfile> data))
            {
                var keys = data.Keys;
                foreach (var key in keys)
                {
                    if (data.TryGetValue(key, out PetExperienceProfile profile) && profile.Active)
                    {
                        var stats = profile.Stats;
                        int level = profile.Level;
                        string colorLevel = White(level.ToString());
                        string maxhealth = White(stats[0].ToString());
                        string attackspeed = White(Math.Round(stats[1], 2).ToString());
                        string primaryattackspeed = White(Math.Round(stats[2], 2).ToString());
                        string physicalpower = White(stats[3].ToString());
                        string spellpower = White(stats[4].ToString());
                        string physcritchance = White(stats[5].ToString());
                        string physcritdamage = White(stats[6].ToString());
                        string spellcritchance = White(stats[7].ToString());
                        string spellcritdamage = White(stats[8].ToString());
                        int avgPower = (int)((stats[3] + stats[4]) / 2);
                        string avgPowerColor = White(avgPower.ToString());
                        float avgCritChance = (float)(((stats[5] + stats[7]) / 2));
                        string formattedAvgCritChance = $"{Math.Round(avgCritChance * 100, 2)}%";
                        string avgCritChanceColor = White(formattedAvgCritChance);
                        float avgCritDamage = ((stats[6] + stats[8]) / 2);
                        string formattedAvgCritDamage = $"{Math.Round(avgCritDamage * 100, 2)}%";
                        string avgCritDamageColor = White(formattedAvgCritDamage);
                        ctx.Reply($"Level: {colorLevel}, Max Health: {maxhealth}, Cast Speed: {attackspeed}, Primary Attack Speed: {primaryattackspeed}, Power: {avgPowerColor}, Critical Chance: {avgCritChanceColor}, Critical Damage: {avgCritDamageColor}");
                        if (DataStructures.PetBuffMap.TryGetValue(platformId, out var keyValuePairs))
                        {
                            string input = key;
                            string pattern = @"PrefabGuid\((-?\d+)\)"; // Pattern to match PrefabGuid(-number)

                            Match match = Regex.Match(input, pattern);
                            if (match.Success)
                            {
                                // Extracted number is in the first group (groups are indexed starting at 1)
                                string extractedNumber = match.Groups[1].Value;
                                //Console.WriteLine($"Extracted Number: {extractedNumber}");

                                // Optionally convert to a numeric type
                                int guidhash = int.Parse(extractedNumber);
                                if (keyValuePairs.TryGetValue(guidhash, out var buffs))
                                {
                                    if (buffs.TryGetValue("Buffs", out var buff) && profile.Level == 80)
                                    {
                                        List<string> buffNamesList = [];
                                        foreach (var buffName in buff)
                                        {
                                            PrefabGUID prefabGUID = new(buffName);
                                            string colorBuff = VCreate.Core.Toolbox.FontColors.Cyan(prefabGUID.GetPrefabName());
                                            buffNamesList.Add(colorBuff);
                                        }
                                        // Join all formatted buff names with a separator (e.g., ", ") to create a single string
                                        string allBuffsOneLine = string.Join(", ", buffNamesList);

                                        // Print the concatenated string of buff names

                                        ctx.Reply($"Active Buffs: {allBuffsOneLine}");
                                    }
                                    if (buffs.TryGetValue("Shiny", out var shiny))
                                    {
                                        PrefabGUID prefabGUID = new(shiny.First());
                                        string colorShiny = VCreate.Core.Toolbox.FontColors.Pink(prefabGUID.GetPrefabName());
                                        ctx.Reply($"Shiny Buff: {colorShiny}");
                                    }
                                }
                            }
                        }
                        return;
                    }
                }
                ctx.Reply("Couldn't find active familiar in followers.");
            }
        }

        [Command(name: "toggleFamiliar", shortHand: "toggle", usage: ".toggle", description: "Calls or dismisses familar.", adminOnly: false)]
        public static void ToggleFam(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;
            if (!Services.PlayerService.TryGetPlayerFromString(ctx.Event.User.CharacterName.ToString(), out var player)) return;
            VCreate.Hooks.EmoteSystemPatch.CallDismiss(player, platformId);
        }

        [Command(name: "combatModeToggle", shortHand: "combat", adminOnly: false, usage: ".combat", description: "Toggles combat mode for familiar.")]
        public static void CombatModeToggle(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;
            if (!Services.PlayerService.TryGetPlayerFromString(ctx.Event.User.CharacterName.ToString(), out var player)) return;
            VCreate.Hooks.EmoteSystemPatch.ToggleCombat(player, platformId);
        }

        [Command(name: "shinyToggle", shortHand: "shiny", adminOnly: false, usage: ".shiny", description: "Toggles shiny buff for familiar if unlocked.")]
        public static void ShinyToggle(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;

            if (DataStructures.PlayerSettings.TryGetValue(platformId, out var settings))
            {
                if (settings.Shiny)
                {
                    settings.Shiny = false;
                    DataStructures.SavePlayerSettings();
                    ctx.Reply("Shiny buff disabled.");
                }
                else
                {
                    settings.Shiny = true;
                    DataStructures.SavePlayerSettings();
                    ctx.Reply("Shiny buff enabled.");
                }
            }
            else
            {
                ctx.Reply("Couldn't find data to toggle shiny.");
            }
        }

        [Command(name: "beginTrade", shortHand: "trade", adminOnly: true, usage: ".trade [Name]", description: "Trades unlocked unit, including shiny buff, to other players.")]
        public static void StartTrade(ChatCommandContext ctx, string name)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            // both fams need to be active and not in stasis for this to work so people see what they're getting, maybe do a proximity check too
            ulong starterId = ctx.User.PlatformId;

            // validate user input
            bool check = VCreate.Core.Services.PlayerService.TryGetPlayerFromString(name, out var player);
            if (!check)
            {
                ctx.Reply("Couldn't find user to trade with.");
                return;
            }
            // make sure user isn't trading with themselves
            if (starterId == player.SteamID)
            {
                ctx.Reply("You can't trade with yourself.");
                return;
            }
            ulong traderId = player.SteamID;

            // verify first user has familiar set and active (not in stasis) and prevent from dismissing until trade is concluded, do same on other end
            if (DataStructures.PlayerPetsMap.TryGetValue(starterId, out var keyValuePairs))
            {
                var keys = keyValuePairs.Keys;
                foreach (var key in keys)
                {
                    if (keyValuePairs.TryGetValue(key, out PetExperienceProfile profile) && profile.Active)
                    {
                        if (PetCommands.PlayerFamiliarStasisMap.TryGetValue(starterId, out var value))
                        {
                            if (value.IsInStasis)
                            {
                                ctx.Reply("You have a familiar in stasis. Call it before trading.");
                                return;
                            }
                        }

                        // set trading flag and set with whom
                        if (DataStructures.PlayerSettings.TryGetValue(starterId, out var settings))
                        {
                            var followers = ctx.Event.SenderCharacterEntity.ReadBuffer<FollowerBuffer>();
                            bool flag = false;
                            foreach (var follower in followers)
                            {
                                if (follower.Entity._Entity.Read<PrefabGUID>().GuidHash.Equals(settings.Familiar))
                                {
                                    flag = true; break;
                                }
                            }
                            if (!flag)
                            {
                                ctx.Reply("Couldn't find active familiar in followers for trading.");
                                return;
                            }
                            // notify other player after checking if both players are close enough
                            var distance = ctx.Event.SenderCharacterEntity.Read<Translation>().Value - player.Character.Read<Translation>().Value;
                            // Calculate the magnitude of the distance vector to get the scalar distance
                            var distanceMagnitude = math.length(distance);

                            // If distance is less than 2, set to idle
                            if (distanceMagnitude > 15f)
                            {
                                // too far away, cancel trade
                                ctx.Reply("You are too far away to trade with that player. Get closer and try again.");
                                return;
                            }
                            else
                            {
                                settings.Trading = true;
                                settings.With = traderId;
                                DataStructures.SavePlayerSettings();
                                string starterName = ctx.Event.User.CharacterName.ToString();
                                string accepterName = player.Name;
                                string colorAccepter = VCreate.Core.Toolbox.FontColors.Cyan(accepterName);
                                ctx.Reply($"Trade request sent to {colorAccepter}");
                                ServerChatUtils.SendSystemMessageToClient(entityManager, player.User.Read<User>(), $"{starterName} would like to trade their currently active familiar with yours. Make sure your familiar is following and active before accepting and stay nearby. Use .cancel to decline and .accept to trade.");
                            }
                        }
                        else
                        {
                            ctx.Reply("Couldn't find data to start trade.");
                            return;
                        }
                    }
                }
            }
        }

        [Command(name: "cancelTrade", shortHand: "cancel", adminOnly: true, usage: ".cancel", description: "Cancels trade if you started it, declines trade if you didn't start it.")]
        public static void CancelTrade(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;
            if (DataStructures.PlayerSettings.TryGetValue(platformId, out var settings))
            {
                if (settings.Trading)
                {
                    settings.Trading = false;
                    settings.With = 0;
                    DataStructures.SavePlayerSettings();
                    ctx.Reply("Trade cancelled.");
                }
                else
                {
                    // want to decline trade here if not the person that started it, look for their name in the data of other peoples settings if they're trading
                    foreach (var key in DataStructures.PlayerSettings.Keys)
                    {
                        if (DataStructures.PlayerSettings[key].Trading && DataStructures.PlayerSettings[key].With == platformId)
                        {
                            UserModel userModel = VRising.GameData.GameData.Users.GetUserByPlatformId(key);
                            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, userModel.FromCharacter.User.Read<User>(), $"{ctx.Event.User.CharacterName} has declined your trade request.");
                            DataStructures.PlayerSettings[key].Trading = false;
                            DataStructures.PlayerSettings[key].With = 0;
                            DataStructures.SavePlayerSettings();
                            ctx.Reply("Trade declined.");
                            return;
                        }
                    }
                    ctx.Reply("You don't have any active trade requests to cancel.");
                }
            }
            else
            {
                ctx.Reply("Couldn't find data to cancel trade.");
            }
        }

        [Command(name: "acceptTrade", shortHand: "accept", adminOnly: true, usage: ".accept", description: "Accepts proposed trade.")]
        public static void AcceptTrade(ChatCommandContext ctx)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            ulong accepterId = ctx.User.PlatformId;
            foreach (var key in DataStructures.PlayerSettings.Keys)
            {
                if (DataStructures.PlayerSettings[key].Trading && DataStructures.PlayerSettings[key].With == accepterId)
                {
                    if (DataStructures.PlayerPetsMap.TryGetValue(accepterId, out var keyValuePairs))
                    {
                        var keys = keyValuePairs.Keys;
                        foreach (var pick in keys)
                        {
                            if (keyValuePairs.TryGetValue(pick, out PetExperienceProfile profile) && profile.Active)
                            {
                                if (PetCommands.PlayerFamiliarStasisMap.TryGetValue(accepterId, out var value))
                                {
                                    if (value.IsInStasis)
                                    {
                                        ctx.Reply("You have a familiar in stasis. Summon it before accepting the trade.");
                                        return;
                                    }
                                }

                                Entity accepterFamiliar = FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);
                                if (accepterFamiliar.Equals(Entity.Null))
                                {
                                    ctx.Reply("Couldn't find active familiar to trade.");
                                    return;
                                }

                                // set trading flag and set with whom
                                if (DataStructures.PlayerSettings.TryGetValue(accepterId, out var settings))
                                {
                                    UserModel userModel = VRising.GameData.GameData.Users.GetUserByPlatformId(key);
                                    Entity character = userModel.FromCharacter.Character;
                                    // notify other player after checking if both players are close enough
                                    Entity traderFamiliar = FindPlayerFamiliar(character);
                                    if (traderFamiliar.Equals(Entity.Null))
                                    {
                                        ctx.Reply("Couldn't find familiars to trade.");
                                        return;
                                    }
                                    var distance = ctx.Event.SenderCharacterEntity.Read<Translation>().Value - character.Read<Translation>().Value;
                                    // Calculate the magnitude of the distance vector to get the scalar distance
                                    var distanceMagnitude = math.length(distance);

                                    if (distanceMagnitude > 15f)
                                    {
                                        // too far away, cancel trade
                                        ctx.Reply("You are too far away to trade with that player. Get closer and try again.");
                                        return;
                                    }
                                    else
                                    {
                                        settings.Trading = true;
                                        settings.With = key;
                                        DataStructures.SavePlayerSettings();
                                        string accepterName = ctx.Event.User.CharacterName.ToString();
                                        ServerChatUtils.SendSystemMessageToClient(entityManager, userModel.FromCharacter.User.Read<User>(), $"{accepterName} has accepted your trade offer, trading familiars...");

                                        // begin by swapping the unlock entries in the unlocked pets map, also transfer shiny buffs if applicable
                                        if (DataStructures.UnlockedPets.TryGetValue(accepterId, out var accepterData) && DataStructures.UnlockedPets.TryGetValue(key, out var traderData))
                                        {
                                            PrefabGUID accepterFamiliarGuid = accepterFamiliar.Read<PrefabGUID>();
                                            PrefabGUID traderFamiliarGuid = traderFamiliar.Read<PrefabGUID>();
                                            if (accepterData.Contains(accepterFamiliarGuid.GuidHash) && traderData.Contains(traderFamiliarGuid.GuidHash))
                                            {
                                                if (DataStructures.PetBuffMap.TryGetValue(accepterId, out var accepterBuffData) && DataStructures.PetBuffMap.TryGetValue(key, out var traderBuffData))
                                                {
                                                    try
                                                    {
                                                        if (accepterBuffData.TryGetValue(accepterFamiliarGuid.GuidHash, out var accepterBuffs) && traderBuffData.TryGetValue(traderFamiliarGuid.GuidHash, out var traderBuffs))
                                                        {
                                                            if (accepterBuffs.TryGetValue("Shiny", out var accepterShiny))
                                                            {
                                                                traderBuffs["Shiny"] = accepterShiny;
                                                                traderBuffData[accepterFamiliarGuid.GuidHash] = traderBuffs;
                                                                DataStructures.PetBuffMap[key] = traderBuffData;
                                                                DataStructures.SavePetBuffMap();
                                                                // leave profiles alone
                                                            }
                                                            if (traderBuffs.TryGetValue("Shiny", out var traderShiny))
                                                            {
                                                                accepterBuffs["Shiny"] = traderShiny;
                                                                accepterBuffData[traderFamiliarGuid.GuidHash] = accepterBuffs;
                                                                DataStructures.PetBuffMap[accepterId] = accepterBuffData;
                                                                DataStructures.SavePetBuffMap();
                                                                // leave profiles alone
                                                            }

                                                            if (DataStructures.PlayerSettings.TryGetValue(key, out var traderSettings))
                                                            {
                                                                accepterData.Remove(accepterFamiliarGuid.GuidHash);
                                                                traderData.Remove(traderFamiliarGuid.GuidHash);
                                                                accepterData.Add(traderFamiliarGuid.GuidHash);
                                                                traderData.Add(accepterFamiliarGuid.GuidHash);
                                                                DataStructures.UnlockedPets[accepterId] = accepterData;
                                                                DataStructures.UnlockedPets[key] = traderData;
                                                                DataStructures.SaveUnlockedPets();

                                                                settings.Trading = false;
                                                                settings.With = 0;

                                                                traderSettings.Trading = false;
                                                                traderSettings.With = 0;

                                                                DataStructures.SavePlayerSettings();
                                                                SystemPatchUtil.Destroy(accepterFamiliar);
                                                                SystemPatchUtil.Destroy(traderFamiliar);
                                                                ServerChatUtils.SendSystemMessageToClient(entityManager, userModel.FromCharacter.User.Read<User>(), "Trade successful.");
                                                                ctx.Reply("Trade successful.");
                                                            }
                                                            else
                                                            {
                                                                ctx.Reply("Couldn't verify trade.");
                                                                return;
                                                            }
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        ctx.Reply("Couldn't complete trade, cancelling...");
                                                        ServerChatUtils.SendSystemMessageToClient(entityManager, userModel.FromCharacter.User.Read<User>(), "Couldn't complete trade, cancelling...");
                                                        settings.Trading = false;
                                                        settings.With = 0;
                                                        if (DataStructures.PlayerSettings.TryGetValue(key, out var traderSettings))
                                                        {
                                                            traderSettings.Trading = false;
                                                            traderSettings.With = 0;
                                                        }
                                                        DataStructures.SavePlayerSettings();
                                                        return;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ctx.Reply("Couldn't find familiars to trade.");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            ctx.Reply("Couldn't find data to start trade.");
                                            return;
                                        }
                                    }
                                }
                                else
                                {
                                    ctx.Reply("Couldn't find data to start trade.");
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    ctx.Reply("Couldn't find trade to accept.");
                    return;
                }
            }
        }

        /*
        public static void MethodFive(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;
            var buffs = ctx.Event.SenderCharacterEntity.ReadBuffer<BuffBuffer>();

            foreach (var buff in buffs)
            {
                if (buff.PrefabGuid.GuidHash == VCreate.Data.Prefabs.Buff_InCombat.GuidHash)
                {
                    ctx.Reply("You cannot toggle combat mode during combat.");
                    return;
                }
            }

            if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out Dictionary<string, PetExperienceProfile> data))
            {
                ServerGameManager serverGameManager = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
                BuffUtility.BuffSpawner buffSpawner = BuffUtility.BuffSpawner.Create(serverGameManager);
                EntityCommandBufferSystem entityCommandBufferSystem = VWorld.Server.GetExistingSystem<EntityCommandBufferSystem>();
                EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
                Entity familiar = FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);
                if (familiar.Equals(Entity.Null))
                {
                    ctx.Reply("Summon your familiar before toggling this.");
                    return;
                }
                if (data.TryGetValue(familiar.Read<PrefabGUID>().LookupName().ToString(), out PetExperienceProfile profile) && profile.Active)
                {
                    profile.Combat = !profile.Combat; // this will be false when first triggered
                    FactionReference factionReference = familiar.Read<FactionReference>();
                    PrefabGUID ignored = new(-1430861195);
                    PrefabGUID player = new(1106458752);
                    if (!profile.Combat)
                    {
                        factionReference.FactionGuid._Value = ignored;
                    }
                    else
                    {
                        factionReference.FactionGuid._Value = player;
                    }

                    //familiar.Write(new Immortal { IsImmortal = !profile.Combat });

                    familiar.Write(factionReference);
                    BufferFromEntity<BuffBuffer> bufferFromEntity = VWorld.Server.EntityManager.GetBufferFromEntity<BuffBuffer>();
                    if (profile.Combat)
                    {
                        //BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, VCreate.Data.Prefabs.AB_Charm_Active_Human_Buff, familiar);
                        AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                        aggroConsumer.Active = ModifiableBool.CreateFixed(true);
                        familiar.Write(aggroConsumer);

                        Aggroable aggroable = familiar.Read<Aggroable>();
                        aggroable.Value = ModifiableBool.CreateFixed(true);
                        aggroable.AggroFactor._Value = 1f;
                        aggroable.DistanceFactor._Value = 1f;
                        familiar.Write(aggroable);
                        BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, VCreate.Data.Prefabs.Admin_Invulnerable_Buff, familiar);
                        BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, VCreate.Data.Prefabs.AB_Militia_HoundMaster_QuickShot_Buff, familiar);
                    }
                    else
                    {
                        AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                        aggroConsumer.Active = ModifiableBool.CreateFixed(false);
                        familiar.Write(aggroConsumer);

                        Aggroable aggroable = familiar.Read<Aggroable>();
                        aggroable.Value = ModifiableBool.CreateFixed(false);
                        aggroable.AggroFactor._Value = 0f;
                        aggroable.DistanceFactor._Value = 0f;
                        familiar.Write(aggroable);
                        OnHover.BuffNonPlayer(familiar, VCreate.Data.Prefabs.Admin_Invulnerable_Buff);
                        OnHover.BuffNonPlayer(familiar, VCreate.Data.Prefabs.AB_Militia_HoundMaster_QuickShot_Buff);
                    }

                    data[familiar.Read<PrefabGUID>().LookupName().ToString()] = profile;
                    DataStructures.PlayerPetsMap[platformId] = data;
                    DataStructures.SavePetExperience();
                    if (!profile.Combat)
                    {
                        string disabledColor = VCreate.Core.Toolbox.FontColors.Pink("disabled");
                        ctx.Reply($"Combat for familiar is {disabledColor}. It cannot die and won't participate, however, no experience will be gained.");
                    }
                    else
                    {
                        string enabledColor = VCreate.Core.Toolbox.FontColors.Green("enabled");
                        ctx.Reply($"Combat for familiar is {enabledColor}. It will fight till glory or death and gain experience.");
                    }
                }
                else
                {
                    ctx.Reply("Couldn't find active familiar in followers.");
                }
            }
            else
            {
                ctx.Reply("You don't have any familiars.");
                return;
            }
        }
        */

        internal struct FamiliarStasisState
        {
            public Entity FamiliarEntity;
            public bool IsInStasis;

            public FamiliarStasisState(Entity familiar, bool isInStasis)
            {
                FamiliarEntity = familiar;
                IsInStasis = isInStasis;
            }
        }

        public static Entity FindPlayerFamiliar(Entity characterEntity)
        {
            if (!characterEntity.Has<FollowerBuffer>()) return Entity.Null;
            var followers = characterEntity.ReadBuffer<FollowerBuffer>();
            foreach (var follower in followers)
            {
                if (!follower.Entity._Entity.Has<PrefabGUID>()) continue;
                PrefabGUID prefabGUID = follower.Entity._Entity.Read<PrefabGUID>();
                ulong platformId = characterEntity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                if (DataStructures.PlayerSettings.TryGetValue(platformId, out var data))
                {
                    if (data.Familiar.Equals(prefabGUID.GuidHash))
                    {
                        return follower.Entity._Entity;
                    }
                }
            }
            if (followers.Length != 0) // want to check for invalid followers
            {
                foreach (var follower in followers)
                {
                    if (!follower.Entity._Entity.Has<PrefabGUID>()) continue;
                    return follower.Entity._Entity;
                }
            }
            //Plugin.Log.LogError("Couldn't find familiar.");
            return Entity.Null;
        }
    }
}