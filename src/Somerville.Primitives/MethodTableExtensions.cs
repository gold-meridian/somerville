using System;
using System.Runtime.CompilerServices;

namespace Somerville.Primitives;

public static unsafe class MethodTableExtensions
{
    extension(Type type)
    {
        public MethodTable* AsMethodTable()
        {
            return (MethodTable*)type.TypeHandle.Value;
        }

        public ref MethodTable AsMethodTableRef()
        {
            return ref *(MethodTable*)type.TypeHandle.Value;
        }
    }
}
