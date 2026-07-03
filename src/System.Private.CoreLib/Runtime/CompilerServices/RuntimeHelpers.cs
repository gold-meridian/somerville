// see: coreclr/System.Private.CoreLib/src/System/Runtime/CompilerServices/RuntimeHelpers.CoreCLR.cs

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

public static partial class RuntimeHelpers {
    internal static unsafe extern bool AreTypesEquivalent(MethodTable* pMTa, MethodTable* pMTb);
}

public enum CastResult
{
    CannotCast = 0,
    CanCast = 1,
    MaybeCast = 2
}

// Subset of src\vm\typedesc.h
[StructLayout(LayoutKind.Sequential)]
public unsafe struct TypeDesc
{
    private uint _typeAndFlags;
    private nint _exposedClassObject;

    private const uint enum_flag_IsCollectible = 0x00000100;

    public Type? ExposedClassObject
    {
        get
        {
            return *(Type*)Unsafe.AsPointer(ref _exposedClassObject);
        }
    }

    public bool IsCollectible
    {
        get
        {
            return (_typeAndFlags & enum_flag_IsCollectible) != 0;
        }
    }
}

/// <summary>
/// A type handle, which can wrap either a pointer to a <c>TypeDesc</c> or to a <see cref="MethodTable"/>.
/// </summary>
public readonly unsafe partial struct TypeHandle
{
    // Subset of src\vm\typehandle.h

    /// <summary>
    /// The address of the current type handle object.
    /// </summary>
    private readonly void* m_asTAddr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TypeHandle(void* tAddr)
    {
        m_asTAddr = tAddr;
    }

    /// <summary>
    /// Gets whether the current instance wraps a <see langword="null"/> pointer.
    /// </summary>
    public bool IsNull => m_asTAddr is null;

    /// <summary>
    /// Gets whether or not this <see cref="TypeHandle"/> wraps a <c>TypeDesc</c> pointer.
    /// Only if this returns <see langword="false"/> it is safe to call <see cref="AsMethodTable"/>.
    /// </summary>
    public bool IsTypeDesc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ((nint)m_asTAddr & 2) != 0;
    }

    /// <summary>
    /// Gets the <see cref="MethodTable"/> pointer wrapped by the current instance.
    /// </summary>
    /// <remarks>This is only safe to call if <see cref="IsTypeDesc"/> returned <see langword="false"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MethodTable* AsMethodTable()
    {
        Debug.Assert(!IsTypeDesc);

        return (MethodTable*)m_asTAddr;
    }

    /// <summary>
    /// Gets the <see cref="TypeDesc"/> pointer wrapped by the current instance.
    /// </summary>
    /// <remarks>This is only safe to call if <see cref="IsTypeDesc"/> returned <see langword="true"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TypeDesc* AsTypeDesc()
    {
        Debug.Assert(IsTypeDesc);

        return (TypeDesc*)((nint)m_asTAddr & ~2); // Drop the second lowest bit.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeHandle TypeHandleOf<T>()
    {
        return new TypeHandle((void*)RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle));
    }

    public static bool AreSameType(TypeHandle left, TypeHandle right) => left.m_asTAddr == right.m_asTAddr;

    public extern int GetCorElementType();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanCastTo(TypeHandle destTH)
    {
        return TryCanCastTo(this, destTH) switch
        {
            CastResult.CanCast => true,
            CastResult.CannotCast => false,

            // Regular casting does not allow T to be cast to Nullable<T>.
            // See TypeHandle::CanCastTo()
            _ => CanCastToWorker(this, destTH, nullableCast: false)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanCastToForReflection(TypeHandle srcTH, TypeHandle destTH)
    {
        return TryCanCastTo(srcTH, destTH) switch
        {
            CastResult.CanCast => true,
            CastResult.CannotCast => false,

            // Reflection allows T to be cast to Nullable<T>.
            // See ObjIsInstanceOfCore()
            _ => CanCastToWorker(srcTH, destTH, nullableCast: true)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static extern CastResult TryCanCastTo(TypeHandle srcTH, TypeHandle destTH);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static extern bool CanCastToWorker(TypeHandle srcTH, TypeHandle destTH, bool nullableCast);
}
