using System.Runtime.Serialization;
using Unity.Entities;

namespace ProjectM;

/// <summary>
/// Provides a lightweight stand-in for the <c>ServerScriptMapper</c> DOTS type so tests can resolve
/// expected services without loading the native runtime.
/// </summary>
public class ServerScriptMapper
{
    /// <summary>
    /// Gets or sets the stubbed <see cref="ServerGameManager"/> instance returned by
    /// <see cref="GetServerGameManager"/>.
    /// </summary>
    public ServerGameManager? ServerGameManager { get; set; }

    /// <summary>
    /// Returns the configured <see cref="ServerGameManager"/>, creating a placeholder if none has been assigned.
    /// </summary>
    public ServerGameManager GetServerGameManager()
    {
        return ServerGameManager ??= (ServerGameManager)FormatterServices.GetUninitializedObject(typeof(ServerGameManager));
    }

    /// <summary>
    /// Returns a default singleton value for the requested component type.
    /// </summary>
    public T GetSingleton<T>() where T : struct
    {
        return default;
    }

    /// <summary>
    /// Returns a default entity reference for the requested component type.
    /// </summary>
    public Entity GetSingletonEntity<T>() where T : struct
    {
        return default;
    }

    /// <summary>
    /// Returns a default entity reference for the requested accessor type.
    /// </summary>
    public Entity GetSingletonEntityFromAccessor<T>() where T : struct
    {
        return default;
    }
}
