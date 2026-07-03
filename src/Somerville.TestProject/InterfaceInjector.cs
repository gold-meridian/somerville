using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MonoMod.RuntimeDetour;
using Somerville.Primitives;

[assembly: IgnoresAccessChecksTo("System.Private.CoreLib")]

namespace Somerville.TestProject;

public static unsafe class InterfaceInjector
{
    // Records a modification to TargetType's MethodTable
    private unsafe readonly struct InjectionRecord
    {
        public required Type TargetType { get; init; }
        public required Type InjectedType { get; init; }
        public required MethodTable** OriginalInterfaceMap { get; init; }
        public required ushort OriginalInterfaceCount { get; init; }

        public MethodTable* OriginalParentMethodTable { get; init; }

        // We don't update ParentMethodTable when updating the ifacemap of types deriving TargetType, so it is valid for InjectionRecord to not track the original parent method table
        public bool HasOriginalParentMethodTable()
        {
            return OriginalParentMethodTable != null;
        }
    }

    private static AssemblyName SyntheticParentAsmName = new("InterfaceInjection.SyntheticParents");
    private static AssemblyBuilder SyntheticParentsAsm
    {
        get
        {
            field ??= AssemblyBuilder.DefineDynamicAssembly(SyntheticParentAsmName, AssemblyBuilderAccess.Run);
            return field;
        }
    }

    private static ModuleBuilder SyntheticParentsMod
    {
        get
        {
            field ??= SyntheticParentsAsm.DefineDynamicModule(SyntheticParentAsmName.Name!);
            return field;
        }
    }

    private static readonly Stack<InjectionRecord> Ledger = new();

    private static delegate*<void*, bool, ObjectHandleOnStack, bool> delegate_IsInstanceOf_NoCacheLookup = &CastHelpers.IsInstanceOf_NoCacheLookup;

    private static readonly List<IDetour> MMHooks = [
      new NativeHook((nint)delegate_IsInstanceOf_NoCacheLookup, Hooks.IsInstanceOf_NoCacheLookup, false)
    ];

    public static void Init()
    {
        foreach (var hook in MMHooks)
        {
            hook.Apply();
        }
    }

    public static void Deinit()
    {
        foreach (var record in Ledger)
        {
            var targetMT = record.TargetType.AsMethodTable();
            
            var newInterfaceMap = targetMT->InterfaceMap;
            targetMT->InterfaceMap = record.OriginalInterfaceMap;
            targetMT->InterfaceCount = record.OriginalInterfaceCount;
            Marshal.FreeHGlobal((nint)newInterfaceMap);

            if (record.HasOriginalParentMethodTable())
            {
                targetMT->ParentMethodTable = record.OriginalParentMethodTable;
            }
        }

        foreach (var hook in MMHooks)
        {
            hook.Undo();
        }

        // TODO: clean up the dynamic assembly?
    }

    private static class Hooks
    {
        public delegate bool orig_IsInstanceOf_NoCacheLookup(void* toTypeHnd, bool throwCastException, ObjectHandleOnStack obj);
        
        //delegate*<void*, bool, ObjectHandleOnStack, bool> orig
        // TODO are these TypeHandles or RuntimeTypeHandles? Is there a difference?
        public static bool IsInstanceOf_NoCacheLookup(orig_IsInstanceOf_NoCacheLookup orig, void* toTypeHnd, bool throwCastException, ObjectHandleOnStack obj)
        {
            throw new Exception("is this thing on?");
            return orig(toTypeHnd, throwCastException, obj) switch
            {
                false => ShouldBeInstanceOf(new TypeHandle(toTypeHnd).FindMethodTable(), Unsafe.AsRef<object>(obj._ptr).GetType().AsMethodTable()),
                true => true,
            };
        }
    }

    private static bool ShouldBeInstanceOf(MethodTable* objType, MethodTable* chkType)
    {
        // objType will be null once we have fully walked up the inheritance hierarchy
        if (objType == null) {
            return false;
        }

        foreach (var record in Ledger)
        {
            // TODO is comparing MT pointers sound?
            if (record.TargetType.AsMethodTable() == objType && record.InjectedType.AsMethodTable() == chkType)
            {
                // TODO insert into the originally checked type's MethodTable's InterfaceMap at this point
                return true;
            }
        }

        // TODO what is MethodTable::GetMethodTableMatchingParentClass()?
        return ShouldBeInstanceOf(objType->ParentMethodTable, chkType);
    }

    extension (TypeHandle handle) {
        public static TypeHandle TypeHandleOf(Type type)
        {
            return new TypeHandle((void*)RuntimeTypeHandle.ToIntPtr(type.TypeHandle));
        }

        public MethodTable* FindMethodTable()
        {
            if (handle.IsNull)
            {
                return null;
            }

            return handle.IsTypeDesc switch {
                // TODO is this NotNull assertion sound?
                true => handle.AsTypeDesc()->ExposedClassObject!.AsMethodTable(),
                false => handle.AsMethodTable(),
            };
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

    // TODO would be more convenient to pass in a delegate, but then we would have to keep it around somewhere
    public static void InjectInterface(Type targetType, Type injectedType, params ReadOnlySpan<(MethodInfo, MethodInfo)> defs)
    {
        if (targetType.FullName is null)
        {
            throw new ArgumentException("Type must not be a generic type.", nameof(targetType));
        } else if (targetType.BaseType == null)
        {
            // TODO: can we lift the restriction on not injecting into interfaces?
            throw new ArgumentException("Type must not be object or an interface.", nameof(targetType));
        }

        var syntheticParentImplBuilder = SyntheticParentsMod.DefineType($"__IMPL__.{targetType.FullName}", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        syntheticParentImplBuilder.AddInterfaceImplementation(injectedType);

        foreach (var (decl, def) in defs)
        {
            // TODO: ensure that decl and def have compatible signatures

            var implBuilder = syntheticParentImplBuilder.DefineMethod($"{injectedType.Name}.{decl.Name}", MethodAttributes.Virtual | MethodAttributes.Final, typeof(int), null);
            var il = implBuilder.GetILGenerator();

            if (!decl.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }

            for (int i = 0; i < decl.GetParameters().Length; i++)
            {
                il.Emit(OpCodes.Ldarg, 1 + i);
            }

            il.Emit(OpCodes.Call, def);

            if (decl.ReturnType != typeof(void))
            {
                il.Emit(OpCodes.Ret);
            }

            syntheticParentImplBuilder.DefineMethodOverride(implBuilder, decl);
        }

        var syntheticParentImplType = syntheticParentImplBuilder.CreateType();

        var syntheticParentBuilder = SyntheticParentsMod.DefineType($"__PARENT__.{targetType.FullName}", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract, targetType.BaseType);
        syntheticParentBuilder.AddInterfaceImplementation(syntheticParentImplType);
        var syntheticParentType = syntheticParentBuilder.CreateType();

        var targetMT = targetType.AsMethodTable();
        var syntheticParentMT = syntheticParentType.AsMethodTable();
        var injectedMT = injectedType.AsMethodTable();

        var originalParentMethodTable = targetMT->ParentMethodTable;

        targetMT->ParentMethodTable = syntheticParentMT;

        var oldCount = targetMT->InterfaceCount;
        var newCount = oldCount + 1;
        var newInterfaceMapPtr = (MethodTable**)Marshal.AllocHGlobal(newCount * sizeof(MethodTable*));

        for (var i = 0; i < oldCount; i++)
        {
            newInterfaceMapPtr[i] = targetMT->InterfaceMap[i];
        }

        newInterfaceMapPtr[oldCount] = injectedMT;

        var originalInterfaceMap = targetMT->InterfaceMap;
        var originalInterfaceCount = targetMT->InterfaceCount;

        targetMT->InterfaceMap = newInterfaceMapPtr;
        targetMT->InterfaceCount = (ushort)newCount;

        Ledger.Push(new()
        {
            TargetType = targetType,
            InjectedType = injectedType,
            OriginalInterfaceMap = originalInterfaceMap,
            OriginalInterfaceCount = originalInterfaceCount,
            OriginalParentMethodTable = originalParentMethodTable,
        });
    }
}
