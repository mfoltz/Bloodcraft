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

    static readonly Regex regex = new(@"^\[\d+\]:");
    static readonly Regex regexMAC = new(@";mac([^;]+)$");
    static readonly byte[] sharedKey = Convert.FromBase64String("c2VjdXJlLXN1cGVyLXNlY3JldC1rZXktaGVyZQ==");

    [HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ChatMessageSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_661171423_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!ConfigService.ClientCompanion) continue;

                ChatMessageEvent chatMessageEvent = entity.Read<ChatMessageEvent>();
                string message = chatMessageEvent.MessageText.Value;
                //Core.Log.LogInfo($"Received message: {message}");

                if (VerifyMAC(message, out string originalMessage))
                {
                    //Core.Log.LogInfo($"Verified message, handling...");
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
        // Separate the original message and the MAC
        Match match = regexMAC.Match(receivedMessage);
        originalMessage = "";

        if (match.Success)
        {
            //Core.Log.LogInfo("MAC found, verifying...");
            string receivedMAC = match.Groups[1].Value;
            string intermediateMessage = regexMAC.Replace(receivedMessage, "");
            string recalculatedMAC = GenerateMAC(intermediateMessage);
            //Core.Log.LogInfo($"Received MAC: {receivedMAC} | Recalculated MAC: {recalculatedMAC}");

            // Compare the MACs
            if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recalculatedMAC), Encoding.UTF8.GetBytes(receivedMAC)))
            {
                originalMessage = intermediateMessage;
                return true;
            }
        }
        return false;
    }
    public static string GenerateMAC(string message)
    {
        using var hmac = new HMACSHA256(sharedKey);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
