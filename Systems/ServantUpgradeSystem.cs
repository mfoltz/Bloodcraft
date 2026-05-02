using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Core;
using static Bloodcraft.Utilities.VEvents;
using static Bloodcraft.VExtensions;

namespace Bloodcraft.Systems;

// 'https://github.com/Odjit/KindredCommands/blob/d956d65a3f05322c4430130b272d2d64cc906265/Commands/ServantCommands.cs#L76' - Helpful reference material <3
public sealed class ServantUpgradeSystem : SystemBase
{
    EntityQuery _servantQuery;
    EntityTypeHandle _entityHandle;

    ComponentTypeHandle<ServantConnectedCoffin> _coffinHandle;
    ComponentTypeHandle<ServantPower> _powerHandle;

    ComponentLookup<ServantCoffinstation> _stationLookup;
    ComponentLookup<UserOwner> _ownerLookup;
    ComponentLookup<User> _userLookup;

    const float QUALITY = 100f;
    const float EXPERTISE = 0.50f;
    const float POWER = 20f;

    public override void OnCreate()
    {
        Enabled = true;
        OnCreateInternal();
    }

    void OnCreateInternal()
    {
        _entityHandle = GetEntityTypeHandle();
        _coffinHandle = GetComponentTypeHandle<ServantConnectedCoffin>(true);
        _powerHandle = GetComponentTypeHandle<ServantPower>(false);

        _stationLookup = GetComponentLookup<ServantCoffinstation>(true);
        _ownerLookup = GetComponentLookup<UserOwner>(true);
        _userLookup = GetComponentLookup<User>(true);

        _servantQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { Il2CppTypeOf<ServantConnectedCoffin>(), Il2CppTypeOf<ServantPower>() },
            None = new ComponentType[] { Il2CppTypeOf<BlockFeedBuff>() },
            Options = EntityQueryOptions.IncludeDisabled
        });
        RequireForUpdate(_servantQuery);
    }

    void OnBeforeUpdate()
    {
        _entityHandle.Update(this);
        _coffinHandle.Update(this);
        _powerHandle.Update(this);

        _stationLookup.Update(this);
        _ownerLookup.Update(this);
        _userLookup.Update(this);
    }

    public override void OnUpdate()
    {
        if (!TryReceive(out ServantUpgradeEvent servantUpgradeEvent))
            return;

        OnBeforeUpdate();
        OnReceive(ref servantUpgradeEvent);

        /*
        ServantUpgradeJob servantUpgradeJob = new()
        {
            EntityTypeHandle = _entityHandle,
            CoffinHandle = _coffinHandle,
            PowerHandle = _powerHandle,
            PlayerName = servantUpgradeEvent.Player,
            ServantName = servantUpgradeEvent.Servant,
            Quality = QUALITY,
            Expertise = EXPERTISE,
            Power = POWER
        };
        */
    }

    void OnReceive(ref ServantUpgradeEvent servantUpgradeEvent)
    {
        var chunks = _servantQuery.ToArchetypeChunkArray(Allocator.Temp);
        bool wasUpgraded = false;

        try
        {
            foreach (var chunk in chunks)
            {
                var servants = chunk.GetNativeArray(_entityHandle);
                var connectedCoffins = chunk.GetNativeArray(_coffinHandle);
                var powers = chunk.GetNativeArray(_powerHandle);

                for (int i = 0; i < chunk.Count; ++i)
                {
                    Entity servant = servants[i];
                    ServantPower servantPower = powers[i];

                    ServantConnectedCoffin servantConnectedCoffin = connectedCoffins[i];
                    Entity servantCoffin = servantConnectedCoffin.CoffinEntity.GetEntityOnServer();

                    if (!Exists(servant)
                        || !_stationLookup.TryGetComponent(servantCoffin, out ServantCoffinstation servantCoffinStation)
                        || !_ownerLookup.TryGetComponent(servantCoffin, out UserOwner userOwner)
                        || !userOwner.Owner.TryGetSyncedEntity(out Entity userEntity))
                    {
                        continue;
                    }

                    if (!Exists(userEntity)
                        || !_userLookup.TryGetComponent(userEntity, out User user))
                    {
                        continue;
                    }

                    FixedString64Bytes playerName = user.CharacterName;
                    FixedString64Bytes servantName = servantCoffinStation.ServantName;

                    bool isPlayer = !playerName.IsEmpty && playerName.Value.Equals(servantUpgradeEvent.Player);
                    bool isServant = !servantName.IsEmpty && servantName.Value.Equals(servantUpgradeEvent.Servant);

                    if (isPlayer && isServant)
                    {
                        servantCoffinStation.BloodQuality = QUALITY;
                        servantCoffinStation.ServantProficiency = EXPERTISE;

                        servantPower.Power = POWER;
                        servantPower.Expertise = EXPERTISE;

                        SetComponent(servantCoffin, servantCoffinStation);
                        SetComponent(servant, servantPower);

                        wasUpgraded = true;
                        KeepReceipt(ref servantUpgradeEvent, wasUpgraded);
                        Log.LogInfo($"Upgraded {servantUpgradeEvent.Servant} for {servantUpgradeEvent.Player}!");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"{ex}");
        }
        finally
        {
            if (chunks.IsCreated)
                chunks.Dispose();
        }

        if (!wasUpgraded)
            KeepReceipt(ref servantUpgradeEvent, wasUpgraded);
    }

    /*
    public struct ServantUpgradeJob : ChunkJobs.IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;

        public ComponentTypeHandle<ServantConnectedCoffin> CoffinHandle;
        public ComponentTypeHandle<ServantPower> PowerHandle;

        public ComponentLookup<ServantCoffinstation> StationLookup;
        public ComponentLookup<UserOwner> OwnerLookup;

        public FixedString64Bytes PlayerName;
        public FixedString64Bytes ServantName;

        public float Quality;
        public float Expertise;
        public float Power;

        public bool IsComplete;
        public void Execute(ref ArchetypeChunk chunk)
        {
            Log.LogWarning($"ServantUpgradeJob - Processing chunk with {chunk.Count} entities");
            var servants = chunk.GetNativeArray(EntityTypeHandle);
            var coffinStations = chunk.GetNativeArray(CoffinHandle);
            var servantPowers = chunk.GetNativeArray(PowerHandle);

            for (int i = 0; i < chunk.Count; ++i)
            {
                Log.LogWarning($"ServantUpgradeJob - Processing entity {i}/{chunk.Count}");
                Entity servant = servants[i];
                ServantConnectedCoffin coffin = coffinStations[i];
                ServantPower servantPower = servantPowers[i];

                if (ShouldProceed(ref coffin,
                    out Entity coffinStation, out ServantCoffinstation servantCoffinStation,
                    out FixedString64Bytes playerName, out FixedString64Bytes servantName)
                    && ShouldUpgrade(ref playerName, ref servantName))
                {
                    Log.LogWarning($"Upgrading servant {servantName} for player {playerName} with Quality={Quality}, Expertise={Expertise}, Power={Power}");
                    servantCoffinStation.BloodQuality = Quality;
                    servantCoffinStation.ServantProficiency = Expertise;

                    servantPower.Power = Power;
                    servantPower.Expertise = Expertise;


                    IsComplete = true;
                    break;
                }
            }
        }

        bool ShouldUpgrade(ref FixedString64Bytes playerName, ref FixedString64Bytes servantName)
        {
            bool isPlayer = HasEqualValue(playerName.Value, PlayerName.Value);
            bool isServant = HasEqualValue(servantName.Value, ServantName.Value);

            return isPlayer && isServant;
        }
    }

    bool ShouldProceed(ref ServantConnectedCoffin servantConnectedCoffin,
        out Entity coffinStation, out ServantCoffinstation servantCoffinStation,
        out FixedString64Bytes playerName, out FixedString64Bytes servantName)
    {
        coffinStation = servantConnectedCoffin.CoffinEntity.GetEntityOnServer();

        servantCoffinStation = default;
        playerName = default;
        servantName = default;

        if (!Instance.HasComponent<ServantCoffinstation>(coffinStation) || !HasComponent<UserOwner>(coffinStation))
            return false;

        servantCoffinStation = Instance.GetComponent<ServantCoffinstation>(coffinStation);
        servantName = servantCoffinStation.ServantName;
        playerName = GetUserName(coffinStation);

        return true;
    }

    static bool HasEqualValue(string source, string target)
        => !string.IsNullOrEmpty(source) && source.Equals(target);

    FixedString64Bytes GetUserName(Entity coffin)
    {
        UserOwner userOwner = GetComponent<UserOwner>(coffin);
        Entity userEntity = userOwner.Owner.GetEntityOnServer();

        if (!HasComponent<User>(coffin))
            return default;

        User user = GetComponent<User>(userEntity);
        return user.CharacterName;
    }
    */
}
