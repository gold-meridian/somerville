using System;
using Somerville.TestProject;

var sanity = new SanityB();
Console.WriteLine($"SANITY: {sanity is IBar}");

var baz = new Baz();
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Baz)));
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Foo)));
Console.WriteLine($"Baz::BaseType: {baz.GetType().BaseType?.AssemblyQualifiedName}");
Console.WriteLine($"Baz::BaseType.BaseType: {baz.GetType().BaseType?.BaseType?.AssemblyQualifiedName}");
Console.WriteLine($"baz is IBar = {baz is IBar}");
Console.WriteLine($"baz.HatedNumber() = {baz.HatedNumber()}");

Console.WriteLine();

InterfaceInjector.InjectInterface(typeof(Foo), typeof(IBar), typeof(IBar).GetMethod("FavouriteNumber")!);
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Baz)));
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Foo)));

Console.WriteLine($"Baz::BaseType: {baz.GetType().BaseType!.AssemblyQualifiedName}");
Console.WriteLine($"Baz::BaseType.BaseType: {baz.GetType().BaseType?.BaseType?.AssemblyQualifiedName}");
Console.WriteLine($"baz is IBar = {baz is IBar}");

// var method = typeof(Foo).GetMethod("IBar.FavouriteNumber", BindingFlags.Instance | BindingFlags.NonPublic);
// var ifaceMethod = typeof(IBar).GetMethod("FavouriteNumber")!;
// Console.WriteLine($"decl invocation: {method.Invoke(foo, [])}");
// Console.WriteLine($"def invocation: {ifaceMethod.Invoke(foo, [])}");
Console.WriteLine($"baz.HatedNumber() = {baz.HatedNumber()}");
Console.WriteLine($"((IBar)(object)baz).FavouriteNumber() = {((IBar)(object)baz).FavouriteNumber()}");

// Console.WriteLine();

// Console.WriteLine($"decl fptr: {method.MethodHandle.GetFunctionPointer()}");
// Console.WriteLine($"def fptr: {ifaceMethod.MethodHandle.GetFunctionPointer()}");
// Console.WriteLine($"natural fptr: {((Func<int>)((IBar)(object)foo).FavouriteNumber).Method.MethodHandle.GetFunctionPointer()}");

// void WriteArr<T>(T[] arr) {
//     Console.Write("[ ");
//     foreach (var elem in arr) {
//         Console.Write(elem?.ToString() ?? "null");
//         Console.Write(", ");
//     }
//     Console.WriteLine("]");
// }

public class Foo
{
    public int HatedNumber()
    {
        return 0xb;
    }
}

public class Baz : Foo { }

public interface IBar
{
    int FavouriteNumber();
}

public interface IBarBar : IBar
{
    int IBar.FavouriteNumber()
    {
        return 44;
    }
}

public class SanityA : IBarBar { }

public class SanityB : SanityA { }
