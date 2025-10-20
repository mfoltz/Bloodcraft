using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bloodcraft.Services;
using ConfigInitialization = Bloodcraft.Services.ConfigService.ConfigInitialization;
using Xunit;

namespace Bloodcraft.Tests.Services;

public sealed class ConfigServiceTests
{
    [Fact]
    public void GetConfigValue_ReturnsStringOverride()
    {
        using var fixture = new ConfigOverridesFixture(("LanguageLocalization", "German"));

        string result = ConfigServiceTestAccessor.GetConfigValue<string>("LanguageLocalization");

        Assert.Equal("German", result);
    }

    [Fact]
    public void GetConfigValue_ConvertsStringOverrideToBool()
    {
        using var fixture = new ConfigOverridesFixture(("Eclipsed", "true"));

        bool result = ConfigServiceTestAccessor.GetConfigValue<bool>("Eclipsed");

        Assert.True(result);
    }

    [Fact]
    public void GetConfigValue_ConvertsStringOverrideToInt()
    {
        using var fixture = new ConfigOverridesFixture(("RiftFrequency", "5"));

        int result = ConfigServiceTestAccessor.GetConfigValue<int>("RiftFrequency");

        Assert.Equal(5, result);
    }

    [Fact]
    public void GetConfigValue_ReturnsDefaultValue_WhenNoOverrideIsPresent()
    {
        using var fixture = new ConfigOverridesFixture();
        ConfigInitialization.FinalConfigValues.Remove("LanguageLocalization");

        string result = ConfigServiceTestAccessor.GetConfigValue<string>("LanguageLocalization");
        string expectedDefault = (string)ConfigInitialization.ConfigEntries
            .First(entry => entry.Key == "LanguageLocalization")
            .DefaultValue;

        Assert.Equal(expectedDefault, result);
    }

    private sealed class ConfigOverridesFixture : IDisposable
    {
        private readonly Dictionary<string, object> snapshot;

        public ConfigOverridesFixture(params (string Key, object Value)[] overrides)
        {
            snapshot = new Dictionary<string, object>(ConfigInitialization.FinalConfigValues);

            foreach ((string key, object value) in overrides)
            {
                ConfigInitialization.FinalConfigValues[key] = value;
            }
        }

        public void Dispose()
        {
            ConfigInitialization.FinalConfigValues.Clear();

            foreach ((string key, object value) in snapshot)
            {
                ConfigInitialization.FinalConfigValues[key] = value;
            }
        }
    }

    private static class ConfigServiceTestAccessor
    {
        private static readonly MethodInfo GetConfigValueDefinition = typeof(ConfigService)
            .GetMethod("GetConfigValue", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Could not locate ConfigService.GetConfigValue method.");

        public static T GetConfigValue<T>(string key)
        {
            MethodInfo genericMethod = GetConfigValueDefinition.MakeGenericMethod(typeof(T));
            object? result = genericMethod.Invoke(null, new object?[] { key });

            return (T)result!;
        }
    }
}
