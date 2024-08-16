using Bloodcraft.SystemUtilities.Familiars;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft.Services;

internal class TeamService
{
	static EntityManager EntityManager = Core.EntityManager;
    static PlayerService PlayerService => Core.PlayerService;

    static MapZoneCollection MapZoneCollection = Core.MapZoneCollection;

    static readonly WaitForSeconds Delay = new(5f);
	public TeamService()
	{
		Core.StartCoroutine(TeamUpdateLoop());
	}
	static IEnumerator TeamUpdateLoop()
    {
        while (true)
        {
            Dictionary<string, Entity> users = new(PlayerService.UserCache); // shallow copy for processing online users by playerCharacter entity (don't forget to change back to online only after testing!)

            foreach (Entity entity in users.Values)
            {
                User user = entity.Read<User>();

                //Core.Log.LogInfo($"TeamService handling player: {user.CharacterName.Value}");

                if (!user.LocalCharacter._Entity.Exists()) continue; // skip if character entity is not in the world

                Entity character = user.LocalCharacter._Entity;
                TilePosition tilePos = character.Read<TilePosition>();

                if (CastleTerritoryExtensions.TryGetCastleTerritory(ref MapZoneCollection, ref EntityManager, tilePos.Tile, out CastleTerritory _))
                {
                    //Core.Log.LogInfo($"Found Castle Territory for player position...");
                    if (character.Read<PlayerCharacter>().SmartClanName.IsEmpty) // use castle team
                    {
                        //Core.Log.LogInfo($"Finding castle team...");
                        foreach (TeamAllies ally in entity.Read<TeamReference>().Value._Value.ReadBuffer<TeamAllies>())
                        {
                            if (ally.Value.Has<CastleTeam>())
                            {
                                //Core.Log.LogInfo($"Using castle team...");
                                int castleTeam = ally.Value.Read<TeamData>().TeamValue;
                                UpdateTeam(character, castleTeam);
                            }
                        }
                    }
                    else // use clan team
                    {
                        //Core.Log.LogInfo($"Using clan team...");
                        int clanTeam = user.ClanEntity._Entity.Read<ClanTeam>().TeamValue;
                        UpdateTeam(character, clanTeam);
                    }
                }
                else // if not in a territory use universal value
                {
                    //Core.Log.LogInfo($"Using universal team...");
                    int universalTeam = 0;
                    UpdateTeam(character, universalTeam);
                }
            }

            yield return Delay;
        }
    }
    static void UpdateTeam(Entity character, int teamValue)
    {
        Entity teamEntity = character.Read<TeamReference>().Value._Value;
        if (!teamEntity.Exists()) return;

        TeamData teamData = teamEntity.Read<TeamData>();
        int oldTeamValue = teamData.TeamValue;

        teamData.TeamValue = teamValue;
        teamEntity.Write(teamData);

        Team team = character.Read<Team>();
        team.Value = teamValue;
        character.Write(team);

        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character);
        if (familiar.Exists() && familiar.Has<TeamReference>())
        {
            Entity familiarTeamEntity = familiar.Read<TeamReference>().Value._Value;
            if (!familiarTeamEntity.Exists()) return;

            TeamData familiarTeamData = familiarTeamEntity.Read<TeamData>();
            familiarTeamData.TeamValue = teamValue;
            familiarTeamEntity.Write(familiarTeamData);

            Team familiarTeam = familiar.Read<Team>();
            familiarTeam.Value = teamValue;
            familiar.Write(familiarTeam);
            //Core.Log.LogInfo($"Updated team values for {character.Read<PlayerCharacter>().Name.Value} and familiar: {oldTeamValue}->{teamValue}");
        }
        else
        {
            //Core.Log.LogInfo($"Updated team values for {character.Read<PlayerCharacter>().Name.Value}: {oldTeamValue}->{teamValue}");
        }
    }
}
