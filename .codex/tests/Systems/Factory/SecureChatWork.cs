using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ProjectM.Network;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition mirroring the secure eclipse chat interception flow.
/// </summary>
public sealed class SecureChatWork : ISystemWork
{
    /// <summary>
    /// Delegate used to validate a recalculated message authentication code.
    /// </summary>
    /// <param name="message">Original message text without the appended MAC marker.</param>
    /// <param name="receivedMac">MAC extracted from the received message.</param>
    /// <param name="key">Shared secret used when recalculating the MAC.</param>
    /// <returns><c>true</c> when the MAC matches the recalculated hash.</returns>
    public delegate bool MacVerificationDelegate(string message, string receivedMac, byte[] key);

    static readonly QueryDescription chatMessageQuery = CreateChatMessageQuery();
    static readonly Regex defaultMacRegex = CreateDefaultRegex();

    readonly Regex macRegex;
    readonly MacVerificationDelegate macVerifier;
    readonly Action? destructionCallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecureChatWork"/> class using the default regex and verifier.
    /// </summary>
    public SecureChatWork()
    {
        macRegex = defaultMacRegex;
        macVerifier = DefaultMacVerifier;
        destructionCallback = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecureChatWork"/> class.
    /// </summary>
    /// <param name="macRegex">Optional regex used to extract MAC values from incoming messages.</param>
    /// <param name="macVerifier">Optional delegate used to validate recalculated MAC values.</param>
    /// <param name="destructionCallback">Optional callback invoked when the work is destroyed.</param>
    public SecureChatWork(
        Regex? macRegex = null,
        MacVerificationDelegate? macVerifier = null,
        Action? destructionCallback = null)
    {
        this.macRegex = macRegex ?? defaultMacRegex;
        this.macVerifier = macVerifier ?? DefaultMacVerifier;
        this.destructionCallback = destructionCallback;
    }

    /// <summary>
    /// Gets the regex used to extract MAC markers from received messages.
    /// </summary>
    public Regex MacRegex => macRegex;

    /// <summary>
    /// Gets the delegate used to validate recalculated MAC values.
    /// </summary>
    public MacVerificationDelegate MacVerifier => macVerifier;

    /// <summary>
    /// Gets the chat message query description.
    /// </summary>
    public QueryDescription ChatMessageQuery => chatMessageQuery;

    /// <summary>
    /// Attempts to extract the original message guarded by the appended MAC.
    /// </summary>
    /// <param name="receivedMessage">Message received from the chat system.</param>
    /// <param name="sharedKey">Shared secret used to validate the MAC.</param>
    /// <param name="originalMessage">When successful, contains the original message without the MAC marker.</param>
    /// <returns><c>true</c> when the MAC is valid and the original message could be reconstructed.</returns>
    public bool TryExtractSecureMessage(string receivedMessage, byte[] sharedKey, out string originalMessage)
    {
        if (receivedMessage == null)
            throw new ArgumentNullException(nameof(receivedMessage));
        if (sharedKey == null)
            throw new ArgumentNullException(nameof(sharedKey));

        Match match = macRegex.Match(receivedMessage);
        if (!match.Success)
        {
            originalMessage = string.Empty;
            return false;
        }

        string receivedMac = match.Groups[1].Value;
        string intermediateMessage = macRegex.Replace(receivedMessage, string.Empty);

        if (!macVerifier(intermediateMessage, receivedMac, sharedKey))
        {
            originalMessage = string.Empty;
            return false;
        }

        originalMessage = intermediateMessage;
        return true;
    }

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<ChatMessageEvent>();
        builder.WithOptions(EntityQueryOptions.IncludeDisabled);
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetEntityTypeHandle();
            _ = facade.GetComponentTypeHandle<ChatMessageEvent>(isReadOnly: true);
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
    }

    /// <inheritdoc />
    public void OnDestroy(SystemContext context)
    {
        destructionCallback?.Invoke();
    }

    static QueryDescription CreateChatMessageQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<ChatMessageEvent>();
        builder.WithOptions(EntityQueryOptions.IncludeDisabled);
        return builder.Describe(requireForUpdate: true);
    }

    static Regex CreateDefaultRegex()
    {
        return new Regex(";mac([^;]+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    static bool DefaultMacVerifier(string message, string receivedMac, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] hashBytes = hmac.ComputeHash(messageBytes);
        string recalculatedMac = Convert.ToBase64String(hashBytes);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(recalculatedMac),
            Encoding.UTF8.GetBytes(receivedMac));
    }
}
