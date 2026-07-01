using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Somerville.Primitives;

[assembly: IgnoresAccessChecksTo("System.Private.CoreLib")]

namespace Somerville.TestProject;

public static unsafe class InterfaceInjector
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
        string InnerPrintTree(MethodTable* handle)
        {
            var sb = new StringBuilder();
            sb.Append($"{(nint)handle:X}");
            handle = handle->ParentMethodTable;

            while ((nint)handle != 0)
            {
                sb.Append(" -> ");
                sb.Append($"{(nint)handle:X}");
                handle = handle->ParentMethodTable;
            }

            return sb.ToString();
        }

        var handle = child.AsMethodTable();
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

        var targetMT = targetType.AsMethodTable();
        var syntheticParentMT = syntheticParentType.AsMethodTable();
        var injectedMT = injectedType.AsMethodTable();

        targetMT->ParentMethodTable = syntheticParentMT;

        var oldCount = targetMT->InterfaceCount;
        var newCount = oldCount + 1;
        var newInterfaceMapPtr = (MethodTable**)Marshal.AllocHGlobal(newCount * sizeof(MethodTable*));

        for (var i = 0; i < oldCount; i++)
        {
            newInterfaceMapPtr[i] = targetMT->InterfaceMap[i];
        }

        newInterfaceMapPtr[oldCount] = injectedMT;

        targetMT->InterfaceMap = newInterfaceMapPtr;
        targetMT->InterfaceCount = (ushort)newCount;
    }
}
