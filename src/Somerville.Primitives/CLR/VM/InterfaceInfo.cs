using System;
using System.Runtime.CompilerServices;

namespace Somerville.Primitives.CLR.VM;

public readonly struct InterfaceInfo
{
    private readonly nint handle;

    public InterfaceInfo(nint handle)
    {
        if (handle == 0)
        {
            throw new ArgumentException("Invalid MethodTable address", nameof(handle));
        }

        this.handle = handle;
    }

#region Members
    public ref MethodTable MethodTable => ref GetRef<MethodTable>(0);
#endregion

    private unsafe ref T GetRef<T>(int offset)
        where T : unmanaged
    {
        return ref Unsafe.AsRef<T>((void*)(handle + offset));
    }
}
