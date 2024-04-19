using Bloodstone.API;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using VCreate.Systems;
using VCreate.Core.Toolbox;
using VCreate.Data;
using static VCreate.Core.Services.PlayerService;
using static VCreate.Systems.Enablers;
using VCreate.Hooks;
using Il2CppSystem;
using Unity.Mathematics;
using VCreate.Core.Converters;
using VCreate.Core.Services;
using ProjectM.CastleBuilding;
using Unity.Collections;
using VRising.GameData.Utils;
using VRising.GameData.Models;
using ProjectM.Tiles;
using ProjectM.Terrain;
using UnityEngine.Analytics;

namespace VCreate.Core.Commands
{
    public class CoreCommands
    {
        [Command(name: "optInDestroyNodes", shortHand: "nodes", adminOnly: false, usage: ".nodes", description: "Toggles if destroy nodes will target player territory if found.")]
        public static void ToggleDestroyMyNodes(ChatCommandContext ctx)
        {
            Entity userEntity = ctx.Event.SenderUserEntity;
            User user = VWorld.Server.EntityManager.GetComponentData<User>(userEntity);
            if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out Omnitool data))
            {
                data.RemoveNodes = !data.RemoveNodes;

                DataStructures.SavePlayerSettings();
                string enabledColor = FontColors.Green("enabled");
                string disabledColor = FontColors.Red("disabled");
                ctx.Reply($"DestroyMyNodes when an admin runs the command opt-in: |{(data.RemoveNodes ? enabledColor : disabledColor)}|");
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "equipUnarmedSkills", shortHand: "equip", adminOnly: true, usage: ".equip", description: "Toggles extra skills when switching to unarmed.")]
        public static void ToggleSkillEquip(ChatCommandContext ctx)
        {
            Entity userEntity = ctx.Event.SenderUserEntity;
            User user = VWorld.Server.EntityManager.GetComponentData<User>(userEntity);
            if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out Omnitool data))
            {
                data.EquipSkills = !data.EquipSkills;

                DataStructures.SavePlayerSettings();
                string enabledColor = FontColors.Green("enabled");
                string disabledColor = FontColors.Red("disabled");
                ctx.Reply($"EquipUnarmedSkills: |{(data.EquipSkills ? enabledColor : disabledColor)}|");
                if (!data.EquipSkills) return;
                ctx.Reply("Extra skills will be equipped when switching to unarmed. Turn this off and switch again to clear.");
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "listFamiliarToggles", shortHand: "listemotes", adminOnly: false, usage: ".listemotes", description: "Displays functions of familiar emotes.")]
        public static void ListFamiliarActions(ChatCommandContext ctx)
        {
            //User setter = ctx.Event.User;
            //Entity userEntity = ctx.Event.SenderUserEntity;
            foreach (var toggle in EmoteSystemPatch.emoteActionsArray[1].Keys)
            {
                PrefabGUID prefabGUID = new(toggle);
                ctx.Reply($"{prefabGUID.LookupName()} | {EmoteSystemPatch.emoteActionsArray[1][toggle].Method.Name}");
            }
        }

        [Command(name: "emotesToggle", shortHand: "emotes", adminOnly: false, usage: ".emotes", description: "Familiar commands on emotes toggle.")]
        public static void ToggleEmoteActions(ChatCommandContext ctx)
        {
            //User setter = ctx.Event.User;
            Entity userEntity = ctx.Event.SenderUserEntity;
            User user = VWorld.Server.EntityManager.GetComponentData<User>(userEntity);
            if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out Omnitool data))
            {
                // Toggle the CanEditTiles value
                data.Emotes = !data.Emotes;

                DataStructures.SavePlayerSettings();
                string enabledColor = FontColors.Green("enabled");
                string disabledColor = FontColors.Red("disabled");
                ctx.Reply($"EmoteToggles: |{(data.Emotes ? enabledColor : disabledColor)}|");
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "buildEmotes", shortHand: "build", adminOnly: true, usage: ".build", description: "Toggles using the emote wheel to change action on Q when extra skills for unarmed are equipped.")]
        public static void ToggleBuilding(ChatCommandContext ctx)
        {
            //User setter = ctx.Event.User;
            Entity userEntity = ctx.Event.SenderUserEntity;
            User user = VWorld.Server.EntityManager.GetComponentData<User>(userEntity);
            if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out Omnitool data))
            {
                // Toggle the CanEditTiles value
                data.Build = !data.Build;

                DataStructures.SavePlayerSettings();
                string enabledColor = FontColors.Green("enabled");
                string disabledColor = FontColors.Red("disabled");
                ctx.Reply($"BuildToggle: |{(data.Build ? enabledColor : disabledColor)}|");
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "listBuildToggles", shortHand: "listbuild", adminOnly: true, usage: ".listbuild", description: "Displays what modes emotes will toggle if applicable.")]
        public static void ListBuildActions(ChatCommandContext ctx)
        {
            //User setter = ctx.Event.User;
            //Entity userEntity = ctx.Event.SenderUserEntity;
            foreach (var toggle in EmoteSystemPatch.emoteActionsArray[0].Keys)
            {
                PrefabGUID prefabGUID = new(toggle);
                ctx.Reply($"{prefabGUID.LookupName()} | {EmoteSystemPatch.emoteActionsArray[0][toggle].Method.Name}");
            }
        }

        [Command(name: "moveDismantlePermissions", shortHand: "perms", adminOnly: true, usage: ".perms [Name]", description: "Toggles tile permissions for a player (allows moving or dismantling objects they don't own if it is something that otherwise could be moved or dismantled by the player).")]
        public static void TogglePlayerPermissions(ChatCommandContext ctx, string name)
        {
            User setter = ctx.Event.User;
            TryGetUserFromName(name, out Entity userEntity);
            User user = VWorld.Server.EntityManager.GetComponentData<User>(userEntity);
            if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out Omnitool data))
            {
                data.Permissions = !data.Permissions;

                DataStructures.SavePlayerSettings();
                string enabledColor = FontColors.Green("enabled");
                string disabledColor = FontColors.Red("disabled");
                ctx.Reply($"Permissions {(data.Permissions ? enabledColor : disabledColor)} for {name}.");
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "setTileRotation", shortHand: "rot", adminOnly: true, usage: ".rot [0/90/180/270]", description: "Sets rotation for spawned tiles.")]
        public static void SetTileRotationCommand(ChatCommandContext ctx, int rotation)
        {
            if (rotation != 0 && rotation != 90 && rotation != 180 && rotation != 270)
            {
                ctx.Reply("Invalid rotation. Use 0, 90, 180, or 270 degrees.");
                return;
            }

            User user = ctx.Event.User;
            if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out Omnitool settings))
            {
                settings.SetData("Rotation", rotation);
                DataStructures.SavePlayerSettings();
                ctx.Reply($"Tile rotation set to: {rotation} degrees.");
            }
        }

        [Command(name: "setSnapLevel", shortHand: "snap", adminOnly: true, usage: ".snap [1/2/3]", description: "Sets snap level for spawned tiles.")]
        public static void SetSnappingLevelCommand(ChatCommandContext ctx, int level)
        {
            if (level != 1 && level != 2 && level != 3)
            {
                ctx.Reply("Options are 1 for 2.5u, 2 for 5u, and 3 for 7.5u.");
                return;
            }

            User user = ctx.Event.User;
            if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out Omnitool settings))
            {
                settings.SetData("GridSize", level);
                DataStructures.SavePlayerSettings();
                ctx.Reply($"Tile snapping set to: {OnHover.gridSizes[settings.GetData("GridSize")] - 1}u");
            }
        }

        [Command(name: "setCharacterUnit", shortHand: "char", adminOnly: true, usage: ".char [PrefabGUID]", description: "Sets cloned unit prefab.")]
        public static void SetUnit(ChatCommandContext ctx, int choice)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerSettings.TryGetValue(SteamID, out Omnitool data))
            {
                // Assuming there's a similar check for map icons as there is for tile models
                if (Prefabs.FindPrefab.CheckForMatch(choice))
                {
                    PrefabGUID prefabGUID = new(choice);
                    if (prefabGUID.LookupName().ToLower().Contains("char"))
                    {
                        ctx.Reply($"Character unit set.");
                        data.SetData("Unit", choice);
                        DataStructures.SavePlayerSettings();
                    }
                    else
                    {
                        ctx.Reply("Invalid character unit.");
                    }
                }
                else
                {
                    ctx.Reply("Couldn't find prefab.");
                }
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "setBuff", shortHand: "sb", adminOnly: true, usage: ".sb [PrefabGUID]", description: "Sets buff for buff mode.")]
        public static void SetBuff(ChatCommandContext ctx, int choice)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerSettings.TryGetValue(SteamID, out Omnitool data))
            {
                // Assuming there's a similar check for map icons as there is for tile models
                if (Prefabs.FindPrefab.CheckForMatch(choice))
                {
                    PrefabGUID prefabGUID = new PrefabGUID(choice);
                    if (prefabGUID.LookupName().ToLower().Contains("buff"))
                    {
                        ctx.Reply($"Buff set.");
                        data.SetData("Buff", choice);
                        DataStructures.SavePlayerSettings();
                    }
                    else
                    {
                        ctx.Reply("Invalid buff.");
                    }
                }
                else
                {
                    ctx.Reply("Couldn't find prefab.");
                }
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "setDebuff", shortHand: "sd", adminOnly: true, usage: ".sd [PrefabGUID]", description: "Sets buff for debuff mode.")]
        public static void SetDebuff(ChatCommandContext ctx, int choice)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerSettings.TryGetValue(SteamID, out Omnitool data))
            {
                // Assuming there's a similar check for map icons as there is for tile models
                if (Prefabs.FindPrefab.CheckForMatch(choice))
                {
                    ctx.Reply($"Debuff set.");
                    data.SetData("Debuff", choice);
                    DataStructures.SavePlayerSettings();
                }
                else
                {
                    ctx.Reply("Couldn't find prefab.");
                }
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "setMapIcon", shortHand: "map", adminOnly: true, usage: ".map [PrefabGUID]", description: "Sets map icon to prefab.")]
        public static void SetMapIcon(ChatCommandContext ctx, int choice)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerSettings.TryGetValue(SteamID, out Omnitool data))
            {
                // Assuming there's a similar check for map icons as there is for tile models
                if (Prefabs.FindPrefab.CheckForMatch(choice))
                {
                    PrefabGUID prefabGUID = new PrefabGUID(choice);
                    if (prefabGUID.LookupName().ToLower().Contains("map"))
                    {
                        ctx.Reply($"Map icon set.");
                        data.SetData("MapIcon", choice);
                        DataStructures.SavePlayerSettings();
                    }
                    else
                    {
                        ctx.Reply("Invalid map icon.");
                    }
                }
                else
                {
                    ctx.Reply("Couldn't find prefab.");
                }
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "setTileModel", shortHand: "tm", adminOnly: true, usage: ".tm [PrefabGUID]", description: "Sets tile model to prefab.")]
        public static void SetTileByPrefab(ChatCommandContext ctx, int choice)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerSettings.TryGetValue(SteamID, out Omnitool data))
            {
                if (Prefabs.FindPrefab.CheckForMatch(choice))
                {
                    PrefabGUID prefabGUID = new PrefabGUID(choice);
                    if (prefabGUID.LookupName().ToLower().Contains("tm"))
                    {
                        ctx.Reply($"Tile model set.");
                        data.SetData("Tile", choice);
                        DataStructures.SavePlayerSettings();
                    }
                    else
                    {
                        ctx.Reply("Invalid choice for tile model.");
                    }
                }
                else
                {
                    ctx.Reply("Invalid tile choice.");
                }
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "undoLast", shortHand: "undo", adminOnly: true, usage: ".undo", description: "Destroys the last tile entity placed, up to 10.")]
        public static void UndoCommand(ChatCommandContext ctx)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            User user = ctx.Event.User;
            if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out Omnitool data))
            {
                string lastTileRef = data.PopEntity();
                if (!string.IsNullOrEmpty(lastTileRef))
                {
                    string[] parts = lastTileRef.Split(", ");
                    if (parts.Length == 2 && int.TryParse(parts[0], out int index) && int.TryParse(parts[1], out int version))
                    {
                        Entity tileEntity = new Entity { Index = index, Version = version };
                        if (entityManager.Exists(tileEntity) && tileEntity.Version == version)
                        {
                            SystemPatchUtil.Destroy(tileEntity);
                            ctx.Reply($"Successfully destroyed last tile placed.");
                            DataStructures.SavePlayerSettings();
                        }
                        else
                        {
                            ctx.Reply("The tile could not be found or has already been modified.");
                        }
                    }
                    else
                    {
                        ctx.Reply("Failed to parse the reference to the last tile placed.");
                    }
                }
                else
                {
                    ctx.Reply("You haven't placed any tiles yet or all undos have been used.");
                }
            }
            else
            {
                ctx.Reply("Couldn't find omnitool data.");
            }
        }

        [Command(name: "destroyResourceNodes", shortHand: "destroynodes", adminOnly: true, usage: ".destroynodes", description: "Destroys resources in player territories. Only use this after disabling worldbuild.")]
        public static void DestroyResourcesCommand(ChatCommandContext ctx)
        {
            ResourceFunctions.SearchAndDestroy();
            ctx.Reply("Resource nodes in player territories destroyed. Probably.");
        }

        [Command(name: "destroyTileModels", shortHand: "dtm", adminOnly: true, description: "Destroys tiles in entered radius matching entered full tile model name (ex: TM_ArtisansWhatsit_T01).", usage: ".dtm [TM_Example_01] [Radius]")]
        public static void DestroyTiles(ChatCommandContext ctx, string name, float radius = 5f)
        {
            // Check if a name is not provided or is empty
            if (string.IsNullOrEmpty(name))
            {
                ctx.Error("You need to specify a tile name!");
                return;
            }

            var tiles = Enablers.TileFunctions.ClosestTilesCTX(ctx, radius, name);

            foreach (var tile in tiles)
            {
                SystemPatchUtil.Destroy(tile);
                //UserOwner userOwner = tile.Read<UserOwner>();
                //ulong platformId = userOwner.Owner._Entity.Read<User>().PlatformId;
                //UserModel userModel = VRising.GameData.GameData.Users.GetUserByPlatformId(platformId);
                //string characterName = userModel.CharacterName;
                ctx.Reply($"{tiles.Count} tiles destroyed.");
            }

            if (tiles.Count < 1)
            {
                ctx.Error("Failed to destroy any tiles, are there any in range?");
            }
            else
            {
                ctx.Reply("Tiles have been destroyed!");
            }
        }

        [Command("claimWorldStructures", shortHand: "claimstructs", usage: ".claimstructs", description: "Claim world structures with no owner. Probably.", adminOnly: true)]
        public static void ClaimWorldStructures(ChatCommandContext ctx)
        {
            HashSet<ulong> processed = [];
            int counter = 0;
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong platformId = ctx.Event.User.PlatformId;
            bool includeDisabled = true;
            Team userTeam = character.Read<Team>();
            EntityQuery heartQuery = VWorld.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                        ComponentType.ReadOnly<PrefabGUID>(),
                        ComponentType.ReadOnly<CastleHeart>(),
                        ComponentType.ReadOnly<Pylonstation>(),
                        ComponentType.ReadOnly<CastleHeartConnection>(),
                },
                Options = includeDisabled ? EntityQueryOptions.IncludeDisabled : EntityQueryOptions.Default
            });
            NativeArray<Entity> heartEntities = heartQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var heartEntity in heartEntities)
                {
                    UserOwner heartUserOwner = heartEntity.Read<UserOwner>();
                    ulong heartPlatformId = heartUserOwner.Owner._Entity.Read<User>().PlatformId;
                    if (!heartPlatformId.Equals(platformId)) continue;
                    if (processed.Contains(heartPlatformId)) continue;
                    processed.Add(heartPlatformId);
                    CastleHeartConnection playerCastleHeartConnection = heartEntity.Read<CastleHeartConnection>();
                    CastleHeart playerCastleHeart = heartEntity.Read<CastleHeart>();
                    Entity heartTerritoryEntity = playerCastleHeart.CastleTerritoryEntity;

                    EntityQuery structureQuery = VWorld.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
                    {
                        All = new ComponentType[]
                        {
                            ComponentType.ReadWrite<CastleHeartConnection>(),
                            ComponentType.ReadWrite<Team>(),
                        },
                        Options = includeDisabled ? EntityQueryOptions.IncludeDisabled : EntityQueryOptions.Default
                    });
                    NativeArray<Entity> structureEntities = structureQuery.ToEntityArray(Allocator.Temp);
                    try
                    {
                        foreach (var structure in structureEntities)
                        {
                            if (!structure.Read<Team>().Value.Equals(1) || !structure.Has<UserOwner>()) continue;
                            UserOwner tileUserOwner = structure.Read<UserOwner>();
                            tileUserOwner.Owner._Entity = heartUserOwner.Owner._Entity;
                            structure.Write(tileUserOwner);

                            CastleHeartConnection tileCastleHeartConnection = structure.Read<CastleHeartConnection>();
                            tileCastleHeartConnection.CastleHeartEntity._Entity = heartTerritoryEntity.Read<CastleTerritory>().CastleHeart;
                            structure.Write(tileCastleHeartConnection);

                            Team tileTeam = structure.Read<Team>();
                            tileTeam.Value = userTeam.Value;
                            tileTeam.FactionIndex = character.Read<Team>().FactionIndex;
                            structure.Write(tileTeam);

                            counter++;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Plugin.Log.LogError(ex);
                    }
                    finally
                    {
                        structureEntities.Dispose();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
            finally
            {
                heartEntities.Dispose();
                processed.Clear();
                Plugin.Log.LogInfo($"Claimed {counter} structures.");
            }
        }

        [Command(name: "repairUserStructures", shortHand: "repair", adminOnly: true, usage: ".repair", description: "Restores broken UserOwners, CastleHeartConnections, and Teams based on territory.")]
        public static void RepairUserStructures(ChatCommandContext ctx)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            // iterate through users, find their hearts and the associated territory, and claim all structures in the territory
            var users = VRising.GameData.GameData.Users.All;
            foreach (var player in users)
            {
                User user = player.FromCharacter.User.Read<User>();
                string name = user.CharacterName.ToString();
                ulong platformId = user.PlatformId;
                Entity character = player.FromCharacter.Character;
                if (user.CharacterName.ToString().ToLower().Contains("beta")) continue; // ignore old characters from beta
                bool includeDisabled = true;
                Plugin.Log.LogInfo($"Checking structures for {name}");
                if (character.Equals(Entity.Null)) continue;
                if (!character.Has<Team>()) continue;
                Team userTeam = character.Read<Team>();
                EntityQuery heartQuery = VWorld.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PrefabGUID>(),
                        ComponentType.ReadOnly<CastleHeart>(),
                        ComponentType.ReadOnly<Pylonstation>(),
                        ComponentType.ReadOnly<CastleHeartConnection>(),
                    },
                    Options = includeDisabled ? EntityQueryOptions.IncludeDisabled : EntityQueryOptions.Default
                });
                NativeArray<Entity> heartEntities = heartQuery.ToEntityArray(Allocator.Temp);
                try
                {
                    foreach (var heartEntity in heartEntities)
                    {
                        UserOwner heartUserOwner = heartEntity.Read<UserOwner>();
                        ulong heartPlatformId = heartUserOwner.Owner._Entity.Read<User>().PlatformId;
                        if (!heartPlatformId.Equals(platformId)) continue;
                        CastleHeartConnection playerCastleHeartConnection = heartEntity.Read<CastleHeartConnection>();
                        CastleHeart playerCastleHeart = heartEntity.Read<CastleHeart>();
                        Entity heartTerritoryEntity = playerCastleHeart.CastleTerritoryEntity;

                        EntityQuery structureQuery = VWorld.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
                        {
                            All = new ComponentType[]
                            {
                            ComponentType.ReadWrite<CastleHeartConnection>(),
                            ComponentType.ReadOnly<TilePosition>(),
                            ComponentType.ReadWrite<UserOwner>(),
                            ComponentType.ReadWrite<Team>(),
                            },
                            Options = includeDisabled ? EntityQueryOptions.IncludeDisabled : EntityQueryOptions.Default
                        });
                        NativeArray<Entity> structureEntities = structureQuery.ToEntityArray(Allocator.Temp);
                        try
                        {
                            int counter = 0;
                            foreach (var structure in structureEntities)
                            {
                                if (!CastleTerritoryCache.TryGetCastleTerritory(structure, out Entity territoryEntity)) continue;
                                if (heartTerritoryEntity.Equals(territoryEntity))
                                {
                                    UserOwner tileUserOwner = structure.Read<UserOwner>();
                                    tileUserOwner.Owner._Entity = heartUserOwner.Owner._Entity;
                                    structure.Write(tileUserOwner);

                                    CastleHeartConnection tileCastleHeartConnection = structure.Read<CastleHeartConnection>();
                                    tileCastleHeartConnection.CastleHeartEntity._Entity = heartTerritoryEntity.Read<CastleTerritory>().CastleHeart;
                                    structure.Write(tileCastleHeartConnection);

                                    Team tileTeam = structure.Read<Team>();
                                    tileTeam.Value = userTeam.Value;
                                    tileTeam.FactionIndex = character.Read<Team>().FactionIndex;
                                    structure.Write(tileTeam);

                                    counter++;
                                    if (!structure.Has<CastleBuildingAttachedChildrenBuffer>()) continue;
                                    var childrenBuffer = structure.ReadBuffer<CastleBuildingAttachedChildrenBuffer>();
                                    if (childrenBuffer.IsEmpty || !childrenBuffer.IsCreated) continue;
                                    foreach (var child in childrenBuffer)
                                    {
                                        Entity childEntity = child.ChildEntity._Entity;
                                        //childEntity.LogComponentTypes();
                                        //Plugin.Log.LogInfo($"{childEntity.Read<PrefabGUID>().LookupName()}");
                                        //if (!childEntity.Has<UserOwner>() || !childEntity.Has<CastleHeartConnection>() || !childEntity.Has<Team>()) continue;
                                        if (!childEntity.Has<CastleFloor>()) continue;

                                        UserOwner childUserOwner = childEntity.Read<UserOwner>();
                                        childUserOwner.Owner._Entity = heartUserOwner.Owner._Entity;
                                        childEntity.Write(childUserOwner);

                                        CastleHeartConnection childCastleHeartConnection = childEntity.Read<CastleHeartConnection>();
                                        childCastleHeartConnection.CastleHeartEntity._Entity = heartTerritoryEntity.Read<CastleTerritory>().CastleHeart;
                                        childEntity.Write(childCastleHeartConnection);

                                        Team childTeam = childEntity.Read<Team>();
                                        childTeam.Value = userTeam.Value;
                                        childTeam.FactionIndex = character.Read<Team>().FactionIndex;
                                        childEntity.Write(childTeam);
                                        counter++;
                                    }
                                }
                            }
                            Plugin.Log.LogInfo($"{counter} structures claimed for {user.CharacterName.ToString()}.");
                        }
                        catch (System.Exception ex)
                        {
                            Plugin.Log.LogError(ex);
                        }
                        finally
                        {
                            structureEntities.Dispose();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogError(ex);
                }
                finally
                {
                    heartEntities.Dispose();
                    Plugin.Log.LogInfo($"Claim structures complete for {name}.");
                }
            }
        }

        [Command(name: "logPrefabComponents", shortHand: "logprefab", adminOnly: true, usage: ".logprefab [#]", description: "WIP")]
        public static void LogUnitStats(ChatCommandContext ctx, int prefab)
        {
            PrefabGUID toLog = new(prefab);
            if (toLog.GetPrefabName().Equals(""))
            {
                ctx.Reply("Invalid prefab.");
                return;
            }
            else
            {
                Entity entity = VWorld.Server.GetExistingSystem<PrefabCollectionSystem>()._PrefabGuidToEntityMap[toLog];
                if (entity == Entity.Null)
                {
                    ctx.Reply("Entity not found.");
                    return;
                }
                else
                {
                    entity.LogComponentTypes();
                    ctx.Reply("Components logged.");
                }
            }
        }

        public static void CastCommand(ChatCommandContext ctx, FoundPrefabGuid prefabGuid, FoundPlayer player = null)
        {
            PlayerService.Player player1;
            Entity entity1;
            if ((object)player == null)
            {
                entity1 = ctx.Event.SenderUserEntity;
            }
            else
            {
                player1 = player.Value;
                entity1 = player1.User;
            }
            Entity entity2 = entity1;
            Entity entity3;
            if ((object)player == null)
            {
                entity3 = ctx.Event.SenderCharacterEntity;
            }
            else
            {
                player1 = player.Value;
                entity3 = player1.Character;
            }
            Entity entity4 = entity3;
            FromCharacter fromCharacter = new FromCharacter()
            {
                User = entity2,
                Character = entity4
            };
            DebugEventsSystem existingSystem = VWorld.Server.GetExistingSystem<DebugEventsSystem>();
            CastAbilityServerDebugEvent serverDebugEvent = new CastAbilityServerDebugEvent()
            {
                AbilityGroup = prefabGuid.Value,
                AimPosition = new Nullable_Unboxed<float3>(entity2.Read<EntityInput>().AimPosition),
                Who = entity4.Read<NetworkId>()
            };
            existingSystem.CastAbilityServerDebugEvent(entity2.Read<User>().Index, ref serverDebugEvent, ref fromCharacter);
        }
    }
}