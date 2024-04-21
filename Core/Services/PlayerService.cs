using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Cobalt.Core.Toolbox;

namespace Cobalt.Core.Services;

public static class PlayerService
{
    public struct Player
    {
        public string Name { get; set; }

        public ulong SteamID { get; set; }

        public bool IsOnline { get; set; }

        public bool IsAdmin { get; set; }

        public Entity User { get; set; }

        public Entity Character { get; set; }

        public Player(Entity userEntity = default, Entity charEntity = default)
        {
            User = userEntity;
            User user = User.Read<User>();
            Character = user.LocalCharacter._Entity;
            Name = user.CharacterName.ToString();
            IsOnline = user.IsConnected;
            IsAdmin = user.IsAdmin;
            SteamID = user.PlatformId;
        }
    }

    public static bool TryGetPlayerFromString(string input, out Player player)
    {
        NativeArray<Entity>.Enumerator enumerator = Helper.GetEntitiesByComponentTypes<User>(includeDisabled: true).GetEnumerator();
        while (enumerator.MoveNext())
        {
            Entity current = enumerator.Current;
            User user = current.Read<User>();
            if (user.CharacterName.ToString().ToLower() == input.ToLower())
            {
                player = new Player(current);
                return true;
            }

            if (ulong.TryParse(input, out var result) && user.PlatformId == result)
            {
                player = new Player(current);
                return true;
            }
        }

        player = default;
        return false;
    }

    public static bool TryGetCharacterFromName(string input, out Entity Character)
    {
        if (TryGetPlayerFromString(input, out var player))
        {
            Character = player.Character;
            return true;
        }

        Character = default;
        return false;
    }

    public static bool TryGetUserFromName(string input, out Entity User)
    {
        if (TryGetPlayerFromString(input, out var player))
        {
            User = player.User;
            return true;
        }

        User = default;
        return false;
    }
}
