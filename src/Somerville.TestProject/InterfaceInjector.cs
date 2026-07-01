using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using Somerville.Primitives.CLR.VM;

namespace Somerville.TestProject;

public static class InterfaceInjector
{
    private static AssemblyName SyntheticParentAsmName = new("InterfaceInjection.SyntheticParents");
    private static AssemblyBuilder? _syntheticParentsAsm;

    private static AssemblyBuilder SyntheticParentsAsm
    {
        get
        {
            _syntheticParentsAsm ??= AssemblyBuilder.DefineDynamicAssembly(SyntheticParentAsmName, AssemblyBuilderAccess.Run);
            return _syntheticParentsAsm;
        }
    }

    private static ModuleBuilder? _syntheticParentsMod;

    private static ModuleBuilder SyntheticParentsMod
    {
        get
        {
            _syntheticParentsMod ??= SyntheticParentsAsm.DefineDynamicModule(SyntheticParentAsmName.Name!);
            return _syntheticParentsMod;
        }
    }

    public static string DbgPrintTree(Type child)
    {
        string InnerPrintTree(MethodTable handle)
        {
            var sb = new StringBuilder();
            sb.Append($"{handle.DangerousGetHandle():X}");
            handle = handle.ParentMethodTable;

            while (handle.DangerousGetHandle() != 0)
            {
                sb.Append(" -> ");
                sb.Append($"{handle.DangerousGetHandle():X}");
                handle = handle.ParentMethodTable;
            }

            return sb.ToString();
        }

        var handle = MethodTable.FromType(child);
        return $"{child.FullName}: {InnerPrintTree(handle)}";
    }

    public static void InjectInterface(Type targetType, Type injectedType, params ReadOnlySpan<MethodInfo> defs)
    {
        if (targetType.FullName is null)
        {
            throw new ArgumentException("Type must not be a generic type.", nameof(targetType));
        }

        var syntheticParentImplBuilder = SyntheticParentsMod.DefineType($"__IMPL__.{targetType.FullName}", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        syntheticParentImplBuilder.AddInterfaceImplementation(injectedType);

        foreach (var def in defs)
        {
            var implBuilder = syntheticParentImplBuilder.DefineMethod($"{injectedType.Name}.{def.Name}", MethodAttributes.Virtual | MethodAttributes.Final, typeof(int), null);
            var il = implBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4, 22);
            il.Emit(OpCodes.Ret);
            syntheticParentImplBuilder.DefineMethodOverride(implBuilder, def);
        }

        var syntheticParentImplType = syntheticParentImplBuilder.CreateType();

        var syntheticParentBuilder = SyntheticParentsMod.DefineType($"__PARENT__.{targetType.FullName}", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract, targetType.BaseType);
        syntheticParentBuilder.AddInterfaceImplementation(syntheticParentImplType);
        var syntheticParentType = syntheticParentBuilder.CreateType();

        var targetMT = MethodTable.FromType(targetType);
        var syntheticParentMT = MethodTable.FromType(syntheticParentType);
        var injectedMT = MethodTable.FromType(injectedType);

        targetMT.ParentMethodTable = syntheticParentMT;

        unsafe
        {
            var newSize = targetMT.InterfaceCount + 1;
            var newInterfaceMapPtr = Marshal.AllocHGlobal(newSize * sizeof(nint));
            var newInterfaceMap = new Span<MethodTable>((void*)newInterfaceMapPtr, newSize);
            targetMT.InterfaceMap.CopyTo(newInterfaceMap);

            // Add new interface entry
            newInterfaceMap[targetMT.InterfaceCount] = injectedMT;

            // Update target's MethodTable
            targetMT.InterfaceMapPtr = newInterfaceMapPtr;
            targetMT.InterfaceCount++;
        }
    }
}
