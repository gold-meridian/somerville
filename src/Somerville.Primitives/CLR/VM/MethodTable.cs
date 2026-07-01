using System;
using System.Runtime.CompilerServices;

namespace Somerville.Primitives.CLR.VM;

public readonly struct MethodTable
{
    private static int ParentMethodTableOffset => 0x10 + InternalConstants.DebugClassNamePtr;
    
    private static int InterfaceMapOffset => InternalConstants.Is64Bit ? 0x38 + InternalConstants.DebugClassNamePtr : 0x24 + InternalConstants.DebugClassNamePtr;

    private readonly nint handle;
    
    private MethodTable(nint handle)
    {
        if (handle == 0)
        {
            throw new ArgumentException("Invalid MethodTable address", nameof(handle));
        }
        
        this.handle = handle;
    }

#region Members
    /// <summary>
    ///     The low WORD of the first field is the component size for array and
    ///     string types.
    /// </summary>
    public ref ushort ComponentSize => ref GetRef<ushort>(0);
    
    /// <summary>
    ///     The flags for the current method table (only for not array or string
    ///     types).
    /// </summary>
    public ref uint Flags => ref GetRef<uint>(0);
    
    /// <summary>
    ///     Gets the base size of the type (used when allocating an instance on
    ///     the heap).
    /// </summary>
    public ref uint BaseSize => ref GetRef<uint>(4);
    
    // See additional native members in methodtable.h, not needed here yet.
    // 0x8: m_dwFlags2 (additional flags and token in the upper 24 bits)
    
    public ref ushort VirtualCount => ref GetRef<ushort>(0x0C);
    
    /// <summary>
    ///     Gets the number of interfaces implemented by this type.
    /// </summary>
    public ref ushort InterfaceCount => ref GetRef<ushort>(0x0E);
    
    // For DEBUG builds, there is a conditional field here (see methodtable.h again).
    // 0x10: debug_m_szClassName (display name of the class, for the debugger)

    /// <summary>
    ///     Gets the parent method table handle.
    /// </summary>
    public ref MethodTable ParentMethodTable => ref GetRef<MethodTable>(ParentMethodTableOffset);
    
    /// <summary>
    ///     Gets the raw interface map pointer.
    /// </summary>
    public ref nint InterfaceMapPtr => ref GetRef<nint>(InterfaceMapOffset);

    /// <summary>
    ///     Gets the interface map for the type.
    /// </summary>
    public Span<MethodTable> InterfaceMap => GetPtrSpan<MethodTable>(InterfaceMapPtr, InterfaceCount);
#endregion
    
    /// <summary>
    ///     Gets whether the type has a component size (i.e., is an array or
    ///     string).
    /// </summary>
    public bool HasComponentSize => (Flags & 0x80000000) != 0;

    /// <summary>
    ///     Gets whether the type contains GC pointers.
    /// </summary>
    public bool ContainsGcPointers => (Flags & 0x01000000) != 0;

    /// <summary>
    ///     Gets whether the type is an interface.
    /// </summary>
    public bool IsInterface => (Flags & 0x000C0000) == 0x000C0000;

    /// <summary>
    ///     Gets whether the type is a value type.
    /// </summary>
    public bool IsValueType => (Flags & 0x000C0000) == 0x00040000;

    /// <summary>
    ///     Gets whether the type is a nullable type.
    /// </summary>
    public bool IsNullable => (Flags & 0x000F0000) == 0x00050000;

    /// <summary>
    ///     Gets the rank of a multidimensional array, or 0 if not an array.
    /// </summary>
    public int MultiDimensionalArrayRank
    {
        get
        {
            if (!HasComponentSize)
            {
                return 0;
            }

            return (int)((BaseSize - (uint)(3 * nint.Size)) / (2 * sizeof(int)));
        }
    }

    public nint DangerousGetHandle()
    {
        return handle;
    }

    private unsafe ref T GetRef<T>(int offset)
        where T : unmanaged
    {
        return ref Unsafe.AsRef<T>((void*)(handle + offset));
    }

    private unsafe Span<T> GetPtrSpan<T>(nint ptr, int count)
        where T : unmanaged
    {
        return new Span<T>((void*)ptr, count);
    }
    
    /// <summary>
    ///     Determines whether two <see cref="MethodTable"/>s refer to the same
    ///     type.
    /// </summary>
    public static bool AreSameType(MethodTable mt1, MethodTable mt2)
    {
        return mt1.handle == mt2.handle;
    }

#region Factories
    public static MethodTable FromType(Type type)
    {
        if (type.IsPointer)
        {
            var mt = FromType(type.GetElementType()!);
            {
                // TODO: necessary?
                // mt.SetIsPointer();
            }
            return mt;
        }

        if (type.IsByRef)
        {
            return FromType(type.GetElementType()!);
        }

        return new MethodTable(type.TypeHandle.Value);
    }
#endregion
}
