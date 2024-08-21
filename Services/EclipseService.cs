using Bloodcraft.SystemUtilities.Experience;
using Bloodcraft.SystemUtilities.Expertise;
using Bloodcraft.SystemUtilities.Legacies;
using ProjectM;
using ProjectM.Network;
using System.Text.RegularExpressions;
using Unity.Entities;

namespace Bloodcraft.Services;
internal static class EclipseService
{
    static EntityManager EntityManager => Core.EntityManager;
    static ConfigService ConfigService => Core.ConfigService;
    static PlayerService PlayerService => Core.PlayerService;
    static LocalizationService LocalizationService => Core.LocalizationService;

    static readonly Regex regex = new(@"^\[(\d+)\]:");

    public static HashSet<ulong> RegisteredUsers = [];
    public enum NetworkEventSubType
    {
        RegisterUser,
        ProgressToClient
    }
    public static void HandleClientMessage(string message)
    {
        int eventType = int.Parse(regex.Match(message).Groups[1].Value);
        switch (eventType)
        {
            case (int)NetworkEventSubType.RegisterUser:
                ulong steamId = ulong.Parse(regex.Replace(message, ""));
                RegisterUser(steamId);
                break;
        }
    }
    static void RegisterUser(ulong steamId)
    {
        Dictionary<string, Entity> UserCache = new(PlayerService.UserCache);

        bool isValid = UserCache
            .Values
            .Any(userEntity => userEntity.Read<User>().PlatformId == steamId);

        RegisteredUsers.Add(steamId);
    }
    public static void SendClientProgress(Entity character, ulong SteamID)
    {
        Entity userEntity = character.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();

        int experience = 0;
        int legacy = 0;
        int expertise = 0;
        int legacyEnum = 0;
        int expertiseEnum = 0;

        if (ConfigService.LevelingSystem)
        {
            experience = LevelingSystem.GetLevelProgress(SteamID);
        }

        if (ConfigService.BloodSystem)
        {
            BloodSystem.BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(character.Read<Blood>().BloodType);
            IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
            if (bloodHandler != null)
            {
                legacy = BloodSystem.GetLevelProgress(SteamID, bloodHandler);
                legacyEnum = (int)bloodType;
            }
        }

        if (ConfigService.ExpertiseSystem)
        {
            WeaponSystem.WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(character.Read<Equipment>().WeaponSlot.SlotEntity._Entity);
            IExpertiseHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
            if (expertiseHandler != null)
            {
                expertise = WeaponSystem.GetLevelProgress(SteamID, expertiseHandler);
                expertiseEnum = (int)weaponType;
            }
        }

        string message = $"[{(int)NetworkEventSubType.ProgressToClient}]:{experience:D2},{legacy:D2},{legacyEnum:D2},{expertise:D2},{expertiseEnum:D2}";
        LocalizationService.HandleServerReply(EntityManager, user, message);
    }
}
