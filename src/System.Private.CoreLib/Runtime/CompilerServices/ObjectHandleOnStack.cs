// see: libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/QCallHandles.cs

namespace System.Runtime.CompilerServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
public unsafe ref struct ObjectHandleOnStack
{
    public object* _ptr;

    private ObjectHandleOnStack(object* pObject)
    {
        _ptr = pObject;
    }

    internal static ObjectHandleOnStack Create<T>(ref T o) where T : class?
    {
        return new ObjectHandleOnStack((object*)Unsafe.AsPointer(ref o));
    }
}
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
