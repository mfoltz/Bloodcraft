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

    static readonly Regex RegexMAC = new(@";mac([^;]+)$");

    //static readonly byte[] sharedKey = Convert.FromBase64String("c2VjdXJlLXN1cGVyLXNlY3JldC1rZXktaGVyZQ==");
    static readonly byte[] sharedKey = Convert.FromBase64String("c2VjdXJlLXN1cGVyLUNlY3JldC1rZLktaGVyZQ==");

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
        // Separate the original message and the MAC
        Match match = RegexMAC.Match(receivedMessage);
        originalMessage = "";

        if (match.Success)
        {
            string receivedMAC = match.Groups[1].Value;
            string intermediateMessage = RegexMAC.Replace(receivedMessage, "");
            string recalculatedMAC = GenerateMAC(intermediateMessage);

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
