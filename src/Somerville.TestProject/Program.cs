using System;
using Somerville.TestProject;

InterfaceInjector.Init();

var sanity = new SanityB();
Console.WriteLine($"SANITY: {sanity is IBar}");

var foo = new Foo();
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

Console.WriteLine($"baz.HatedNumber() = {baz.HatedNumber()}");
Console.WriteLine($"((IBar)(object)foo).FavouriteNumber() = {((IBar)(object)foo).FavouriteNumber()}");
_ = ((IBar)(object)baz).FavouriteNumber();
Console.WriteLine($"((IBar)(object)baz).FavouriteNumber() = {((IBar)(object)baz).FavouriteNumber()}");

Console.WriteLine();

InterfaceInjector.Deinit();
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Baz)));
Console.WriteLine(InterfaceInjector.DbgPrintTree(typeof(Foo)));

Console.WriteLine($"Baz::BaseType: {baz.GetType().BaseType!.AssemblyQualifiedName}");
Console.WriteLine($"Baz::BaseType.BaseType: {baz.GetType().BaseType?.BaseType?.AssemblyQualifiedName}");
Console.WriteLine($"baz is IBar = {baz is IBar}");
Console.WriteLine($"((IBar)(object)foo).FavouriteNumber() = {((IBar)(object)foo).FavouriteNumber()}");

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
