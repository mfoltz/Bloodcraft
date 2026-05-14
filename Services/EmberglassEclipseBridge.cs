using Bloodcraft.Patches;
using BloodcraftEclipseBridge.Messages;
using ProjectM.Network;
using System.Reflection;

namespace Bloodcraft.Services;

internal static class EmberglassEclipseBridge
{
    const string EMBERGLASS_ASSEMBLY_NAME = "Emberglass";
    const string VNETWORK_TYPE_NAME = "Emberglass.API.Shared.VNetwork";

    static bool _initialized;
    static bool _available;
    static bool _disabledForSession;
    static bool _unavailableLogged;
    static MethodInfo _sendToClient;
    static PropertyInfo _isReady;
    static EventInfo _onReady;
    static EventInfo _onClientReady;
    static readonly HashSet<string> _loggedSendFailures = [];

    public static void Initialize()
    {
        if (_initialized || !ConfigService.UseEmberglassEclipseBridge || _disabledForSession)
        {
            return;
        }

        _initialized = true;

        if (!TryResolveVNetwork(out Type vNetworkType))
        {
            LogUnavailable("Emberglass is not loaded");
            return;
        }

        try
        {
            MethodInfo registerServerbound = GetGenericMethod(vNetworkType, "RegisterServerbound", 1);
            _sendToClient = GetGenericMethod(vNetworkType, "SendToClient", 2);
            _isReady = vNetworkType.GetProperty("IsReady", BindingFlags.Public | BindingFlags.Static);
            _onReady = vNetworkType.GetEvent("OnReady", BindingFlags.Public | BindingFlags.Static);
            _onClientReady = vNetworkType.GetEvent("OnClientReady", BindingFlags.Public | BindingFlags.Static);

            registerServerbound
                .MakeGenericMethod(typeof(EclipseRegistrationPacket))
                .Invoke(null, [new Action<User, EclipseRegistrationPacket>(OnRegistrationPacket)]);

            _available = true;
            Core.Log.LogInfo("[EclipseBridge:Emberglass] registered");
        }
        catch (Exception ex)
        {
            DisableForSession($"failed to register bridge ({ex.GetType().Name}: {ex.Message})");
        }
    }

    public static void SendToClientOrFallback(User user, string message, string messageKind)
    {
        if (TrySendToClient(user, message, messageKind))
        {
            return;
        }

        LocalizationService.HandleServerReply(Core.EntityManager, user, message);
    }

    static bool TrySendToClient(User user, string message, string messageKind)
    {
        if (!ConfigService.UseEmberglassEclipseBridge || _disabledForSession)
        {
            return false;
        }

        Initialize();

        if (!_available || !IsReady())
        {
            return false;
        }

        try
        {
            _sendToClient
                .MakeGenericMethod(typeof(EclipseServerMessagePacket))
                .Invoke(null, [user, new EclipseServerMessagePacket(message)]);

            return true;
        }
        catch (Exception ex)
        {
            LogSendFailure(messageKind, ex);
            return false;
        }
    }

    static void OnRegistrationPacket(User sender, EclipseRegistrationPacket packet)
    {
        if (string.IsNullOrWhiteSpace(packet.Message))
        {
            Core.Log.LogWarning("[EclipseBridge:Emberglass] empty registration packet received");
            return;
        }

        if (!ChatMessageSystemPatch.CheckMAC(packet.Message, out string originalMessage))
        {
            Core.Log.LogWarning("[EclipseBridge:Emberglass] failed to verify registration MAC");
            return;
        }

        Core.Log.LogInfo("[EclipseBridge:Emberglass] registration received");
        EclipseService.HandleClientMessage(originalMessage);
    }

    static bool TryResolveVNetwork(out Type vNetworkType)
    {
        Assembly assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(loadedAssembly => loadedAssembly.GetName().Name == EMBERGLASS_ASSEMBLY_NAME);

        vNetworkType = assembly?.GetType(VNETWORK_TYPE_NAME, throwOnError: false);
        return vNetworkType != null;
    }

    static MethodInfo GetGenericMethod(Type declaringType, string name, int parameterCount)
    {
        return declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == name
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == parameterCount);
    }

    static bool IsReady()
    {
        return _isReady?.GetValue(null) is true;
    }

    static void LogUnavailable(string reason)
    {
        if (_unavailableLogged)
        {
            return;
        }

        _unavailableLogged = true;
        Core.Log.LogInfo($"[EclipseBridge:Emberglass] unavailable; using ChatMessage bridge ({reason})");
    }

    static void DisableForSession(string reason)
    {
        _available = false;
        _disabledForSession = true;
        Core.Log.LogWarning($"[EclipseBridge:Emberglass] disabled for this session; using ChatMessage bridge ({reason})");
    }

    static void LogSendFailure(string messageKind, Exception exception)
    {
        string formattedException = FormatExceptionForLog(exception);
        string failureKey = $"{messageKind}:{formattedException}";

        if (!_loggedSendFailures.Add(failureKey))
        {
            return;
        }

        Core.Log.LogWarning($"[EclipseBridge:Emberglass] failed to send {messageKind}; using ChatMessage fallback for this message ({formattedException})");
    }

    static string FormatExceptionForLog(Exception exception)
    {
        if (exception is TargetInvocationException { InnerException: not null } targetInvocationException)
        {
            return $"{exception.GetType().Name}: {exception.Message}; inner={FormatExceptionForLog(targetInvocationException.InnerException)}";
        }

        return $"{exception.GetType().Name}: {exception.Message}";
    }
}
