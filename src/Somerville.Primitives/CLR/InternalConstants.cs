using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Somerville.Primitives.CLR;

internal static class InternalConstants
{
    public static bool IsDebuggable { get; } = typeof(RuntimeHelpers).Assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITTrackingEnabled ?? false;

    public static bool Is64Bit => nint.Size == 8;
    
    public static int DebugClassNamePtr => IsDebuggable ? Is64Bit ? 8 : 4 : 0;
}
