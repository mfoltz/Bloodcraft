using System;
using Il2CppInterop.Runtime.Injection;

namespace Bloodcraft.Tests.Stubs;

/// <summary>
/// Provides a no-op detour provider so IL2CPP bootstrapping can be satisfied without native hooks.
/// </summary>
internal sealed class NullDetourProvider : IDetourProvider
{
    public IDetour Create<TDelegate>(IntPtr target, TDelegate replacement) where TDelegate : Delegate
    {
        return new NullDetour(target);
    }

    sealed class NullDetour : IDetour, IDisposable
    {
        readonly IntPtr target;

        public NullDetour(IntPtr target)
        {
            this.target = target;
        }

        public IntPtr Target => target;

        public IntPtr Detour => IntPtr.Zero;

        public IntPtr OriginalTrampoline => target;

        public void Apply()
        {
        }

        public T GenerateTrampoline<T>() where T : Delegate
        {
            return default!;
        }

        public void Dispose()
        {
        }
    }
}
