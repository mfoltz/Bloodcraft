using System.Security.Cryptography;
using System.Text;
using ProjectM.Network;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class SecureChatWorkTests
{
    static byte[] CreateDeterministicKey()
    {
        return Encoding.UTF8.GetBytes("SecureChatSharedKey-Deterministic");
    }

    static string ComputeMac(string message, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hashBytes);
    }

    [Fact]
    public void DescribeQuery_RequestsChatMessageEventWithIncludeDisabled()
    {
        var description = FactoryTestUtilities.DescribeQuery<SecureChatWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(ChatMessageEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.IncludeDisabled, description.Options);
        Assert.True(description.RequireForUpdate);
    }

    [Fact]
    public void OnCreate_RegistersChatMessageEventHandle()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<SecureChatWork>();

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Equal(1, registrar.EntityTypeHandleRequests);

        Assert.Collection(
            registrar.ComponentTypeHandles,
            request =>
            {
                Assert.Equal(typeof(ChatMessageEvent), request.ElementType);
                Assert.True(request.IsReadOnly);
            });

        Assert.Empty(registrar.ComponentLookups);
        Assert.Empty(registrar.BufferTypeHandles);
        Assert.Empty(registrar.BufferLookups);
    }

    [Fact]
    public void TryExtractSecureMessage_ReturnsOriginalWhenMacMatches()
    {
        var key = CreateDeterministicKey();
        var originalMessage = "ECLIPSE|stage=umbra";
        var mac = ComputeMac(originalMessage, key);
        var securePayload = $"{originalMessage};mac{mac}";
        var work = FactoryTestUtilities.CreateWork<SecureChatWork>();

        bool result = work.TryExtractSecureMessage(securePayload, key, out var reconstructed);

        Assert.True(result);
        Assert.Equal(originalMessage, reconstructed);
    }

    [Fact]
    public void TryExtractSecureMessage_ReturnsFalseWhenMacDoesNotMatch()
    {
        var key = CreateDeterministicKey();
        var alternateKey = Encoding.UTF8.GetBytes("SecureChatSharedKey-Alternate");
        var originalMessage = "ECLIPSE|stage=umbra";
        var mac = ComputeMac(originalMessage, key);
        var securePayload = $"{originalMessage};mac{mac}";
        var work = FactoryTestUtilities.CreateWork<SecureChatWork>();

        bool result = work.TryExtractSecureMessage(securePayload, alternateKey, out var reconstructed);

        Assert.False(result);
        Assert.Equal(string.Empty, reconstructed);
    }

    [Fact]
    public void OnDestroy_InvokesConfiguredCallback()
    {
        bool destroyed = false;
        var work = new SecureChatWork(destructionCallback: () => destroyed = true);
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);

        FactoryTestUtilities.OnDestroy(work, context);

        Assert.True(destroyed);
    }
}
