using Iced.Intel;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Runtime.Runtime.VersionSpecific.MethodInfo;
using System.Reflection;

namespace Bloodcraft.Utilities;

/// Bloodstone IL2CPP Method Resolver (thank you Deca!)
internal static class MethodResolver
{
    static ulong ExtractTargetAddress(in Instruction instruction)
    {
        return instruction.Op0Kind switch
        {
            OpKind.FarBranch16 => instruction.FarBranch16,
            OpKind.FarBranch32 => instruction.FarBranch32,
            _ => instruction.NearBranchTarget,
        };
    }
    static unsafe IntPtr ResolveMethodPointer(IntPtr methodPointer)
    {
        var stream = new UnmanagedMemoryStream((byte*)methodPointer, 256, 256, FileAccess.Read);
        var codeReader = new StreamCodeReader(stream);

        var decoder = Decoder.Create(IntPtr.Size == 8 ? 64 : 32, codeReader);
        decoder.IP = (ulong)methodPointer.ToInt64();

        Instruction instr = default;
        while (instr.Mnemonic != Mnemonic.Int3)
        {
            decoder.Decode(out instr);

            if (instr.Mnemonic != Mnemonic.Jmp && instr.Mnemonic != Mnemonic.Add)
            {
                return methodPointer;
            }

            if (instr.Mnemonic == Mnemonic.Add)
            {
                if (instr.Immediate32 != 0x10)
                {
                    return methodPointer;
                }
            }

            if (instr.Mnemonic == Mnemonic.Jmp)
                return new IntPtr((long)ExtractTargetAddress(instr));
        }

        return methodPointer;
    }
    public static unsafe IntPtr ResolveFromMethodInfo(INativeMethodInfoStruct methodInfo)
    {
        return ResolveMethodPointer(methodInfo.MethodPointer);
    }
    public static unsafe IntPtr ResolveFromMethodInfo(MethodInfo method)
    {
        var methodInfoField = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method) ?? throw new Exception($"Couldn't obtain method info for {method}");
        var il2cppMethod = UnityVersionHandler.Wrap((Il2CppMethodInfo*)(IntPtr)(methodInfoField.GetValue(null) ?? IntPtr.Zero));
        return il2cppMethod == null ? throw new Exception($"Method info for {method} is invalid") : ResolveFromMethodInfo(il2cppMethod);
    }
}
