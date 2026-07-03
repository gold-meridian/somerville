using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

// see: coreclr/System.Private.CoreLib/src/System/Runtime/CompilerServices/CastHelpers.cs
public static unsafe partial class CastHelpers
{
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
    public static extern bool IsNullableForType(MethodTable* typeMT, MethodTable* boxedMT);
#pragma warning restore CS0626 // Method, operator, or accessor is marked external and has no attributes on it

    [LibraryImport("QCall")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsInstanceOf_NoCacheLookup(void *toTypeHnd, [MarshalAs(UnmanagedType.Bool)] bool throwCastException, ObjectHandleOnStack obj);
}