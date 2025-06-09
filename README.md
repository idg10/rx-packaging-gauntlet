# Rx.NET Packaging Gauntlet

Repros old Rx.NET packaging problems, and validation that they've been fixed.

## Background

The way in which Rx.NET splits its functionality into packages has changed over the project's history, driven by a few factors:

* The changing landscape of supported .NET versions
* Evolution of multi-target mechanisms (e.g., [PCLs](https://learn.microsoft.com/en-us/previous-versions/dotnet/framework/cross-platform/portable-class-library), multi-target NuGet packages, .NET Standard, the [One .NET](https://learn.microsoft.com/en-us/shows/build-2020/bod106) concept introduced by [.NET 5](https://devblogs.microsoft.com/dotnet/introducing-net-5/))
* The problems that multi-target components can cause for plug-in systems such as Visual Studio extensions
* The problems Rx.NET created for itself through its attempts to fix problems

Each of the approaches tried so far has problems. Unfortunately, some of these are subtle and can't be detected by ordinary unit testing. As a result, it's easy for an attempt to fix one problem to cause a regression for problems that earlier changes fixed. For example, Rx 3.1 fixed a problem for plug-ins, but that fix created some new problems, and when Rx 4.0 fixed those new problems, it also reverted the very thing that fixed the plug-in problems. Some coincidental factors meant that Rx 4.0 didn't in fact cause a regression, but because it no longer featured the design change the ruled out the occurrence of that bug, its re-emergence was inevitable: Rx 5.0 has the exact same plug-in bug that was fixed by Rx 3.1, and that bug continues to be present in Rx 6.0.

It took several years for anyone to notice (or, at any rate, to report) this regression. This illustrates that without tests, regressions happen, so we need some automated way to ensure that we can verify that these complex kinds of issue don't recur.


## Relevant issues

The following issues are relevant to packaging problems

### Issue 97: Plug-in Version Mismatches

The actual title of [issue 97, 'NET 4.0 and .NET 4.5. versions need to be signed with different keys'](https://github.com/dotnet/reactive/issues/97) doesn't give much clue as to the nature of the problem, mainly because it is named for a proposed solution, one that Rx.NET never in fact adopted. Here's a better description:

"A .NET Framework plug-in host can end up running with an Rx.NET assembly built for an older version of .NET than it was built against"

For example, suppose you have two plugins, both built with a dependency on Rx.NET 3.0. The first, which we'll call `PlugIn45`, was built to target .NET Framework 4.5, and the second, which we'll call `PlugIn46` was built to target .NET Framework 4.6. Suppose the host is running .NET Framework 4.6. If it loads `Plugin45` and then `Plugin46`, both will end up with the Rx.NET assembly that targets .NET 4.5.

Like many NuGet packages, Rx's `System.Reactive` package contains build for multiple TFMs. In Rx 3.0, the package's `lib` folder has `net45` and `net46` subfolders (amongst others). And the root cause of this bug is that the `System.Reactive.dll` assemblies in each of these subfolders have exactly the same strong name. Because of that, the .NET Framework won't load both of them into the same process.

This is a problem specific to plug-in architectures, because normally, the application build process will decide which assembly to use. If this were just a .NET FX 4.6 app using two NuGet packages both targetting Rx 3.0, the app build process would pick the `net46` assembly. But with plug-ins, there isn't a single app build. The host app gets built of course, but each plug-in has its own build process. Crucially, NuGet package references get resolved at plug-in build time, and the build output for the plug-in includes the chosen `System.Reactive.dll` assembly. Package systems don't resolve packages at plug-in load time; each plug-in has chosen its specific Rx.NET target when it was built. So `PlugIn45` ships with a copy of `net45/System.Reactive.dll`, while `PlugIn46` ships with a copy of `net46/System.Reactive.dll`. Whichever of these plug-ins loads first, theirs is the copy of `System.Reactive.dll` that gets loaded by the CLR. And whichever loads second will ask the CLR to load v3.0.0.0 of the `System.Reactive` assembly, and the CLR will realise it has already loaded an assembly with that exact name, and so the second plug-in gets to use the copy of `System.Reactive.dll` that came from the first plug-in. That'll be the .NET FX 4.5 version, and not the .NET FX 4.6 version that the second plug-in was built against.

This was fixed in Rx 3.1. Unfortunately the fix was reverted in Rx 4.0, but due to a coincidence, the problem happened not to occur. (This was because the specific [targets supplied in Rx 4.0](https://www.nuget.org/packages/System.Reactive/4.0.0#supportedframeworks-body-tab) meant that if you were writing a .NET FX plug-in that used Rx 4.0, it would have to use the `net46` TFM. So it wasn't possible to create a situation in which two differnt plug-ins both use Rx 4.0 but used different .NET FX targets from the Rx NuGet package.)  But the problem came back in Rx 5.0, and remains in Rx 6.0.


### Issue 1745: Self-contained App Bloat

The title of [issue 1745, 'Adding System.Reactive to WinUI3/MAUI app increases package size by 44MB because it includes WPF and WinForms references'](https://github.com/dotnet/reactive/issues/1745), positions this as a MAUI issue, but actually it's much broader. Simply creating a console application and publishing it as self-contained is enough. Here's a project file that will repro the issue:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0-windows10.0.19041</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <PropertyGroup>
    <SelfContained>true</SelfContained>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Reactive" Version="6.0.1" />
  </ItemGroup>

</Project>
```

A simple `Console.WriteLine("Hello, world!");` in `Program.cs` is sufficient. Build this with `dotnet publish`, and the `bin\Release\net9.0-windows10.0.19041\win-x64\publish` folder will be 194MB in size. Without Rx, it's 100MB. (Still large, because it includes a copy of .NET, and the Windows target means we also get 25MB of `Microsoft.Windows.SDK.NET.dll`. But obviously being almost twice the size is a lot worse.)