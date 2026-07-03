using System;
using Somerville.TestProject;

InterfaceInjector.Init();

// pre-injection
var foo = new Foo();
var baz = new Baz();
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Baz)));
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Foo)));
Console.WriteLine($"Baz::BaseType: {baz.GetType().BaseType?.AssemblyQualifiedName}");
Console.WriteLine($"Baz::BaseType.BaseType: {baz.GetType().BaseType?.BaseType?.AssemblyQualifiedName}");
Console.WriteLine($"baz is IBar = {baz is IBar}");
Console.WriteLine($"baz.HatedNumber() = {baz.HatedNumber()}");

Console.WriteLine();

// post-injection
InterfaceInjector.InjectInterface(typeof(Foo), typeof(IBar), (typeof(IBar).GetMethod("FavouriteNumber")!, ((Func<IBar, int>)MethodImpls.MyFavouriteNumber).Method), (typeof(IBar).GetMethod("DoNothing")!, ((Action<IBar>)MethodImpls.DoNothing).Method));
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Baz)));
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Foo)));

Console.WriteLine($"Baz::BaseType: {baz.GetType().BaseType!.AssemblyQualifiedName}");
Console.WriteLine($"Baz::BaseType.BaseType: {baz.GetType().BaseType?.BaseType?.AssemblyQualifiedName}");
Console.WriteLine($"baz is IBar = {baz is IBar}");

Console.WriteLine($"baz.HatedNumber() = {baz.HatedNumber()}");
Console.WriteLine($"((IBar)(object)foo).FavouriteNumber() = {((IBar)(object)foo).FavouriteNumber()}");
// Currently fails
// Console.WriteLine($"((IBar)(object)baz).FavouriteNumber() = {((IBar)(object)baz).FavouriteNumber()}");

Console.WriteLine();

// post-uninjection
InterfaceInjector.Deinit();
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Baz)));
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Foo)));

Console.WriteLine($"Baz::BaseType: {baz.GetType().BaseType!.AssemblyQualifiedName}");
Console.WriteLine($"Baz::BaseType.BaseType: {baz.GetType().BaseType?.BaseType?.AssemblyQualifiedName}");
Console.WriteLine($"baz is IBar = {baz is IBar}");

public static class MethodImpls
{
    public static int MyFavouriteNumber(IBar self)
    {
        return 22;
    }

    public static void DoNothing(IBar self) { }
}

public interface ITwo;
public class Foo : ITwo
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
    void DoNothing();
}
