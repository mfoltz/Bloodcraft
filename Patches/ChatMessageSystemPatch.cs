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
    static readonly Regex _regexMAC = new(";mac([^;]+)$");

    [HarmonyBefore("CrimsonChatFilter")]
    [HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ChatMessageSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!Core.Eclipsed) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        NativeArray<ChatMessageEvent> chatMessageEvents = __instance.EntityQueries[0].ToComponentDataArray<ChatMessageEvent>(Allocator.Temp);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                ChatMessageEvent chatMessageEvent = chatMessageEvents[i];

                if (CheckMAC(chatMessageEvent.MessageText.Value, out string originalMessage))
                {
                    EclipseService.HandleClientMessage(originalMessage);
                    entity.Destroy(true);
                }
            }
        }
        finally
        {
            entities.Dispose();
            chatMessageEvents.Dispose();
        }
    }
    public static bool CheckMAC(string receivedMessage, out string originalMessage)
    {
        Match match = _regexMAC.Match(receivedMessage);
        originalMessage = "";

        if (match.Success)
        {
            string receivedMAC = match.Groups[1].Value;
            string intermediateMessage = _regexMAC.Replace(receivedMessage, "");

            if (VerifyMAC(intermediateMessage, receivedMAC, Core.NEW_SHARED_KEY))
            {
                originalMessage = intermediateMessage;

                return true;
            }
        }

        return false;
    }
    static bool VerifyMAC(string message, string receivedMAC, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        byte[] hashBytes = hmac.ComputeHash(messageBytes);
        string recalculatedMAC = Convert.ToBase64String(hashBytes);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(recalculatedMAC),
            Encoding.UTF8.GetBytes(receivedMAC));
    }
    public static string GenerateMAC(string message)
    {
        using var hmac = new HMACSHA256(Core.NEW_SHARED_KEY);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        byte[] hashBytes = hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hashBytes);
    }
}
