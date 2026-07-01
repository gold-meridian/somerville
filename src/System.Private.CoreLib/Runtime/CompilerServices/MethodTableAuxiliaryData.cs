using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.CompilerServices;

// Subset of src\vm\methodtable.h
[StructLayout(LayoutKind.Sequential)]
public unsafe struct MethodTableAuxiliaryData
{
    private uint Flags;
    private int CachedVersionResilientHashCode;
    private void* LoaderModule;
    private nint ExposedClassObjectRaw;

    private const uint enum_flag_HasCheckedCanCompareBitsOrUseFastGetHashCode = 0x0002; // Whether we have checked the overridden Equals or GetHashCode
    private const uint enum_flag_CanCompareBitsOrUseFastGetHashCode = 0x0004;           // Is any field type or sub field type overridden Equals or GetHashCode

    private const uint enum_flag_Initialized = 0x0001;
    private const uint enum_flag_HasCheckedStreamOverride = 0x0400;
    private const uint enum_flag_StreamOverriddenRead = 0x0800;
    private const uint enum_flag_StreamOverriddenWrite = 0x1000;
    private const uint enum_flag_EnsuredInstanceActive = 0x2000;

    public bool HasCheckedCanCompareBitsOrUseFastGetHashCode => (Flags & enum_flag_HasCheckedCanCompareBitsOrUseFastGetHashCode) != 0;

    public bool CanCompareBitsOrUseFastGetHashCode
    {
        get
        {
            Debug.Assert(HasCheckedCanCompareBitsOrUseFastGetHashCode);
            return (Flags & enum_flag_CanCompareBitsOrUseFastGetHashCode) != 0;
        }
    }

    public bool HasCheckedStreamOverride => (Flags & enum_flag_HasCheckedStreamOverride) != 0;

    public bool IsStreamOverriddenRead
    {
        get
        {
            Debug.Assert(HasCheckedStreamOverride);
            return (Flags & enum_flag_StreamOverriddenRead) != 0;
        }
    }

    public bool IsStreamOverriddenWrite
    {
        get
        {
            Debug.Assert(HasCheckedStreamOverride);
            return (Flags & enum_flag_StreamOverriddenWrite) != 0;
        }
    }

    /*
    public RuntimeType? ExposedClassObject
    {
        get { return *(RuntimeType*)Unsafe.AsPointer(ref ExposedClassObjectRaw); }
    }
    */

    public bool IsClassInited => (Volatile.Read(ref Flags) & enum_flag_Initialized) != 0;

    public bool IsClassInitedAndActive => (Volatile.Read(ref Flags) & (enum_flag_Initialized | enum_flag_EnsuredInstanceActive)) == (enum_flag_Initialized | enum_flag_EnsuredInstanceActive);

    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    [DebuggerStepThrough]
    public ref DynamicStaticsInfo GetDynamicStaticsInfo()
    {
        return ref Unsafe.Subtract(ref Unsafe.As<MethodTableAuxiliaryData, DynamicStaticsInfo>(ref this), 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    [DebuggerStepThrough]
    public ref ThreadStaticsInfo GetThreadStaticsInfo()
    {
        return ref Unsafe.Subtract(ref Unsafe.As<MethodTableAuxiliaryData, ThreadStaticsInfo>(ref this), 1);
    }
    */
}
