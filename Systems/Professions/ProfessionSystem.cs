using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using User = ProjectM.Network.User;

namespace Cobalt.Systems.Professions
{
    public class ProfessionSystem
    {
        private static readonly int ProfessionMultiplier = Plugin.ProfessionMultiplier.Value; // multiplier for profession experience per harvest
        private static readonly float ProfessionConstant = 0.1f; // constant for calculating level from xp
        private static readonly int ProfessionXPPower = 2; // power for calculating level from xp
        private static readonly int MaxProfessionLevel = Plugin.MaxProfessionLevel.Value; // maximum level

        public static void UpdateProfessions(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = Core.Server.EntityManager;
            if (Killer == Victim) return;

            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User user = entityManager.GetComponentData<User>(userEntity);
            ulong SteamID = user.PlatformId;
            if (!Victim.Has<UnitLevel>() || Victim.Has<Movement>()) return;
            //var VictimLevel = entityManager.GetComponentData<UnitLevel>(Victim);

            PrefabGUID prefabGUID = new(0);
            if (entityManager.HasComponent<YieldResourcesOnDamageTaken>(Victim) && entityManager.HasComponent<EntityCategory>(Victim))
            {
                //Victim.LogComponentTypes();
                var yield = Victim.ReadBuffer<YieldResourcesOnDamageTaken>();
                if (yield.IsCreated && !yield.IsEmpty)
                {
                    prefabGUID = yield[0].ItemType;
                }
            }
            else
            {
                return;
            }

            float ProfessionValue = Victim.Read<EntityCategory>().ResourceLevel;
            if (Victim.Read<UnitLevel>().Level > ProfessionValue)
            {
                ProfessionValue = Victim.Read<UnitLevel>().Level;
            }
            //Core.Log.LogInfo($"{Victim.Read<EntityCategory>().ResourceLevel}|{Victim.Read<UnitLevel>().Level} || {Victim.Read<PrefabGUID>().LookupName()}");
            if (ProfessionValue.Equals(0))
            {
                ProfessionValue = 10;
            }

            ProfessionValue = (int)(ProfessionValue * ProfessionMultiplier);

            IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGUID);

            if (handler != null)
            {
                if (handler.GetProfessionName().Contains("Woodcutting"))
                {
                    ProfessionValue *= ProfessionUtilities.GetWoodcuttingModifier(prefabGUID);
                }

                SetProfession(prefabGUID, user, SteamID, ProfessionValue, handler);
                // retrieve level and award bonus drop table item to inventory every 10 levels?
                GiveProfessionBonus(Victim.Read<PrefabGUID>(), Killer, user, SteamID, handler);
            }
            else
            {
                //Core.Log.LogError($"No handler found for profession...");
            }
        }

        public static void GiveProfessionBonus(PrefabGUID prefab, Entity Killer, User user, ulong SteamID, IProfessionHandler handler)
        {
            EntityManager entityManager = Core.Server.EntityManager;
            ServerGameManager serverGameManager = Core.Server.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager;
            PrefabCollectionSystem prefabCollectionSystem = Core.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
            Entity prefabEntity = prefabCollectionSystem._PrefabGuidToEntityMap[prefab];
            int level = GetLevel(SteamID, handler);
            if (!prefabEntity.Has<DropTableBuffer>())
            {
                if (!prefabEntity.Has<DropTableDataBuffer>())
                {
                    Core.Log.LogInfo("No DropTableDataBuffer or DropTableBuffer found on entity...");
                }
                else
                {
                    //process fish drop table here since prefab enters as a DropTableGuid
                    var dropTableDataBuffer = prefabEntity.ReadBuffer<DropTableDataBuffer>();
                    foreach (var dropTableData in dropTableDataBuffer)
                    {
                        Core.Log.LogInfo($"{dropTableData.Quantity} | {dropTableData.ItemGuid.LookupName()} | {dropTableData.DropRate}");
                        prefabEntity = prefabCollectionSystem._PrefabGuidToEntityMap[dropTableData.ItemGuid];
                        if (!prefabEntity.Has<ItemDataDropGroupBuffer>()) continue;
                        var itemDataDropGroupBuffer = prefabEntity.ReadBuffer<ItemDataDropGroupBuffer>();
                        foreach (var itemDataDropGroup in itemDataDropGroupBuffer)
                        {
                            Core.Log.LogInfo($"{itemDataDropGroup.DropItemPrefab.LookupName()} | {itemDataDropGroup.Quantity} | {itemDataDropGroup.Weight}");
                        }
                    }
                }
            }
            else
            {
                var dropTableBuffer = prefabEntity.ReadBuffer<DropTableBuffer>();
                foreach (var drop in dropTableBuffer)
                {
                    Core.Log.LogInfo($"{drop.DropTrigger} | {drop.DropTableGuid.LookupName()}");
                    switch (drop.DropTrigger)
                    {
                        case DropTriggerType.YieldResourceOnDamageTaken:
                            Entity dropTable = prefabCollectionSystem._PrefabGuidToEntityMap[drop.DropTableGuid];
                            var dropTableDataBuffer = dropTable.ReadBuffer<DropTableDataBuffer>();
                            foreach (var dropTableData in dropTableDataBuffer)
                            {
                                if (dropTableData.ItemGuid.LookupName().ToLower().Contains("ingredient"))
                                {
                                    if (serverGameManager.TryAddInventoryItem(Killer, dropTableData.ItemGuid, level))
                                    {
                                        ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"You received {dropTableData.ItemGuid.LookupName()}x<color=white>{level}</color> from {handler.GetProfessionName()}");
                                        break;
                                    }
                                }
                            }
                            break;

                        case DropTriggerType.OnDeath:
                            dropTable = prefabCollectionSystem._PrefabGuidToEntityMap[drop.DropTableGuid];
                            dropTableDataBuffer = dropTable.ReadBuffer<DropTableDataBuffer>();
                            foreach (var dropTableData in dropTableDataBuffer)
                            {
                                prefabEntity = prefabCollectionSystem._PrefabGuidToEntityMap[dropTableData.ItemGuid];
                                if (!prefabEntity.Has<ItemDataDropGroupBuffer>()) continue;
                                var itemDataDropGroupBuffer = prefabEntity.ReadBuffer<ItemDataDropGroupBuffer>();
                                foreach (var itemDataDropGroup in itemDataDropGroupBuffer)
                                {
                                    Core.Log.LogInfo($"{itemDataDropGroup.DropItemPrefab.LookupName()} | {itemDataDropGroup.Quantity} | {itemDataDropGroup.Weight}");
                                }
                            }
                            break;
                    }
                }
            }
        }

        public static void SetProfession(PrefabGUID prefabGUID, User user, ulong steamID, float value, IProfessionHandler handler)
        {
            EntityManager entityManager = Core.Server.EntityManager;

            handler.AddExperience(steamID, value);
            handler.SaveChanges();

            var xpData = handler.GetExperienceData(steamID);
            UpdatePlayerExperience(prefabGUID, entityManager, user, steamID, xpData, value, handler);
        }

        private static void UpdatePlayerExperience(PrefabGUID prefabGUID, EntityManager entityManager, User user, ulong steamID, KeyValuePair<int, float> xpData, float gainedXP, IProfessionHandler handler)
        {
            float newExperience = xpData.Value + gainedXP;
            int newLevel = ConvertXpToLevel(newExperience);
            bool leveledUp = false;

            if (newLevel > xpData.Key)
            {
                leveledUp = true;
                if (newLevel > MaxProfessionLevel)
                {
                    newLevel = MaxProfessionLevel;
                    newExperience = ConvertLevelToXp(MaxProfessionLevel);
                }
            }

            // Update the experience data with the new values
            var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
            handler.UpdateExperienceData(steamID, updatedXPData);

            // Notify player about the changes
            NotifyPlayer(prefabGUID, entityManager, user, steamID, gainedXP, leveledUp, handler);
        }

        private static void NotifyPlayer(PrefabGUID prefabGUID, EntityManager entityManager, User user, ulong steamID, float gainedXP, bool leveledUp, IProfessionHandler handler)
        {
            ServerGameManager serverGameManager = Core.Server.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager;
            //EntityCommandBufferSystem entityCommandBufferSystem = Core.Server.GetExistingSystemManaged<EntityCommandBufferSystem>();
            //EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            PrefabGUID sctType = serverGameManager.SCTTypes.Generic_Type;
            LocalToWorld localToWorld = user.LocalCharacter._Entity.Read<LocalToWorld>();
            string professionName = handler.GetProfessionName();
            if (leveledUp)
            {
                int newLevel = ConvertXpToLevel(handler.GetExperienceData(steamID).Value);
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"{professionName} improved to [<color=white>{newLevel}</color>]");
            }
            else
            {
                if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["ProfessionLogging"])
                {
                    int levelProgress = GetLevelProgress(steamID, handler);
                    //string floating = gainedXP.ToString()+" "+professionName.ToLower();
                    //Entity sct = ProjectM.ScrollingCombatTextMessage.CreateLocal(entityManager, floating, localToWorld.Up, new Unity.Mathematics.float3(0f, 1f, 1f), user.LocalCharacter, gainedXP, sctType);
                    //entityManager.Instantiate(sct);
                    //Core.Log.LogInfo("Attempted to create scrolling combat text...");
                    serverGameManager.CommandBuffer.AllocateIfNotCreated(Allocator.Temp);
                    serverGameManager.CreateScrollingCombatText(gainedXP, sctType, localToWorld.Up, user.LocalCharacter._Entity, user.LocalCharacter._Entity.Read<PlayerCharacter>().UserEntity, prefabGUID);
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"+<color=yellow>{(int)gainedXP}</color> {professionName.ToLower()} (<color=white>{levelProgress}%</color>)");
                }
            }
        }

        private static int ConvertXpToLevel(float xp)
        {
            // Assuming a basic square root scaling for experience to level conversion
            return (int)(ProfessionConstant * Math.Sqrt(xp));
        }

        private static int ConvertLevelToXp(int level)
        {
            // Reversing the formula used in ConvertXpToLevel for consistency
            return (int)Math.Pow(level / ProfessionConstant, ProfessionXPPower);
        }

        private static float GetXp(ulong steamID, IProfessionHandler handler)
        {
            var xpData = handler.GetExperienceData(steamID);
            return xpData.Value;
        }

        private static int GetLevel(ulong steamID, IProfessionHandler handler)
        {
            return ConvertXpToLevel(GetXp(steamID, handler));
        }

        public static int GetLevelProgress(ulong steamID, IProfessionHandler handler)
        {
            float currentXP = GetXp(steamID, handler);
            int currentLevel = GetLevel(steamID, handler);
            int nextLevelXP = ConvertLevelToXp(currentLevel + 1);
            //Core.Log.LogInfo($"Lv: {currentLevel} | xp: {currentXP} | toNext: {nextLevelXP}");
            int percent = (int)(currentXP / nextLevelXP * 100);
            return percent;
        }
    }

    public class ProfessionUtilities
    {
        private static readonly Dictionary<string, int> FishingMultipliers = new()
        {
            { "farbane", 1 },
            { "dunley", 2 },
            { "gloomrot", 3 },
            { "cursed", 4 },
            { "silverlight", 4 }
        };

        private static readonly Dictionary<string, int> WoodcuttingMultipliers = new()
        {
            { "hallow", 2 },
            { "gloom", 3 },
            { "cursed", 4 }
        };

        private static readonly Dictionary<string, int> TierMultiplier = new()
        {
            { "t01", 1 },
            { "t02", 2 },
            { "t03", 3 },
            { "t04", 4 },
            { "t05", 5 },
            { "t06", 6 },
            { "t07", 7 },
            { "t08", 8 },
            { "t09", 9 },
        };

        public static int GetFishingModifier(PrefabGUID prefab)
        {
            foreach (KeyValuePair<string, int> location in FishingMultipliers)
            {
                if (prefab.LookupName().ToLower().Contains(location.Key))
                {
                    return location.Value;
                }
            }
            return 1;
        }

        public static int GetWoodcuttingModifier(PrefabGUID prefab)
        {
            foreach (KeyValuePair<string, int> location in WoodcuttingMultipliers)
            {
                if (prefab.LookupName().ToLower().Contains(location.Key))
                {
                    return location.Value;
                }
            }
            return 1;
        }

        public static int GetTierMultiplier(PrefabGUID prefab)
        {
            foreach (KeyValuePair<string, int> tier in TierMultiplier)
            {
                if (prefab.LookupName().ToLower().Contains(tier.Key))
                {
                    return tier.Value;
                }
            }
            return 1;
        }
    }
}