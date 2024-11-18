using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ChatMessageSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool ClientCompanion = ConfigService.ClientCompanion;

    static readonly Regex RegexMAC = new(@";mac([^;]+)$");

    [HarmonyBefore("gg.deca.Bloodstone")]
    [HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ChatMessageSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                ChatMessageEvent chatMessageEvent = entity.Read<ChatMessageEvent>();
                string message = chatMessageEvent.MessageText.Value;

                if (ConfigService.ClientCompanion && VerifyMAC(message, out string originalMessage))
                {
                    EclipseService.HandleClientMessage(originalMessage);
                    EntityManager.DestroyEntity(entity);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    public static bool VerifyMAC(string receivedMessage, out string originalMessage)
    {
        // Match the MAC using RegexMAC
        Match match = RegexMAC.Match(receivedMessage);
        originalMessage = "";

        if (match.Success)
        {
            string receivedMAC = match.Groups[1].Value;
            string intermediateMessage = RegexMAC.Replace(receivedMessage, "");

            // Recalculate the MAC
            //string recalculatedMAC = GenerateMAC(intermediateMessage);

            // Compare the MACs
            /*
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(recalculatedMAC),
                    Encoding.UTF8.GetBytes(receivedMAC)))
            {
                originalMessage = intermediateMessage;
                return true;
            }
            */

            if (CheckMAC(intermediateMessage, receivedMAC, Core.OLD_SHARED_KEY) ||
                CheckMAC(intermediateMessage, receivedMAC, Core.NEW_SHARED_KEY))
            {
                originalMessage = intermediateMessage;
                return true;
            }
        }

        return false;
    }
    static bool CheckMAC(string message, string receivedMAC, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] hashBytes = hmac.ComputeHash(messageBytes);
        string recalculatedMAC = Convert.ToBase64String(hashBytes);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(recalculatedMAC),
            Encoding.UTF8.GetBytes(receivedMAC));
    }
    public static string GenerateMACV1_1_2(string message)
    {
        using var hmac = new HMACSHA256(Core.OLD_SHARED_KEY);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashBytes);
    }
    public static string GenerateMACV1_2_2(string message)
    {
        using var hmac = new HMACSHA256(Core.NEW_SHARED_KEY);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
