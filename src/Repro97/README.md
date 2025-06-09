# Repro for Issue [#97](https://github.com/dotnet/reactive/issues/97)

Issue [#97](https://github.com/dotnet/reactive/issues/97) afflicts plug-in systems. It was possible for a plug-in to end up using Rx assemblies targetting an older version of the .NET Framework runtime than the plug-in was built against..


## Versions

Reported for [v2.2.5](https://github.com/dotnet/reactive/releases/tag/v2.2.5)

Fixed in [v3.1.0](https://github.com/dotnet/reactive/releases/tag/v3.1.0) by 

## Explanation

The following subsections describe the issue, and how it arises. See the [Reproducing and Verifying Fixes](#reproducing-and-verifying-fixes) section for an explanation of how the code in this folder demonstrates the problems, and shows that they are fixed in later versions.



### The problem

A plug-in built against the .NET Framework 4.5 version of Rx might find itself using the .NET Framework 4.0 version at runtime, resulting in a `MissingMethodException` if it used functionality only available in the .NET FX 4.5 version. This would occur if another plug-in built against the .NET FX 4.0 version had already been loaded.

The root cause is that for each of the assemblies contained in the Rx NuGet packages, there would be two copies either of which _could_ run on .NET FX 4.5, each of which had the same assembly identity (same name, key token, and version number), but with different API surface areas.

For example, if you look at [Rx-Core v2.2.5](https://www.nuget.org/packages/Rx-Core/2.2.5#supportedframeworks-body-tab) you will see that it has `net40` and `net45` components. The NuGet site lists a much wider range of .NET Framework versions on which this package will run, but it highlights those two because the package includes versions specifically for those two runtime versions; all other supported versions end up using either the `net40` or `net45` version. You can see this in the [package explorer](https://nuget.info/packages/Rx-Core/2.2.5)—the package's `lib` folder contains `net40` and `net45` subfolders.

.NET Framework 4.5 (and all subsequent versions of .NET FX) can load either the `net40` or the `net45` version. But why would you ever end up with the older of the two? In a straightforward .NET application that would never happen, because the NuGet packaging rules would determined at build time that the `net45` version is most appropriate: it would only ever select the `net40` version in a project that targets .NET Framework 4.0.

The problem arises when an application supports plug-ins. This creates the possibility that an application running on .NET 4.5 (e.g., Visual Studio 2012) might load a plug-in that was written to run on an older version of the application that used .NET 4.0 (e.g. Visual Studio 2010). Visual Studio supports loading of plug-ins that were written to run on older versions.

Plug-ins create a problem because there is no longer a single build process to determine which particular assembly should be used from a NuGet package. With an application that has loaded 2 plug-ins there are three build processes:

* Host application running on .NET Framework 4.8.2, using `System.Reactive` v6.0.0
* Plug-in A, built for an older version that ran on .NET Framework 4.5 using `Rx-Main` v2.2.5
* Plug-in B, built for a much older version that ran on .NET Framework 4.0 using `Rx-Main` v2.2.5

The crucial fact to understand here is that the output of all three build processes is essentially a folder with a bunch of DLLs. If the plug-ins used the `Rx-Main` NuGet package, there will be no direct record of this fact in their build output. The build process for each plug-in will copy DLLs out of any NuGet packages the plug-in uses, putting those copies in the build output, and not providing any record of where they came from.

So when Plug-in A was built, the build tools will have been targeting .NET Framework 4.5. (The plug-in may have been built many years ago. The fact that it targets .NET FX 4.5 suggests it was probably built before 2015. So .NET Framework 4.8.2 didn't even exist at the moment when Plug-in A was built.) That means that when it looked inside the various NuGet packages that `Rx-Main` depends on, it will have determined that the assemblies in `lib\net45` were the best choice, so it will have copied the files from that folder into the build output.

Similarly when Plug-in B was built, the build tools will have been targeting .NET 4.0. (Since Plug-in B uses Rx v2.2.5, it can have been built no earlier than August 2014, so .NET FX 4.5 definitely existed at that time. But there were good reasons to continue to target .NET 4.0 at that time. Perhaps the plug-in author wanted to support running on Visual Studio 2010, which was still in widespread use at that time.) So the build tools will have determined for each of the various NuGet packages that `Rx-Main` depends on that the assemblies in `lib\net40` were the best choice, so it will have copied the files from that folder into the build output.

Now consider what hapens when the host application loads these plug-ins. Suppose it happens to load Plug-in B first. The host application will tell the .NET assembly resolver to load the plug-in's main assembly. At some point the plug-in will attempt to use Rx, at which point the assembly resolver will see that it wants (amongst other things) `System.Reactive.Core, Version=2.2.5.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35`.

I mentioned above that the host app itself uses Rx, but since it's using v6.0.0, the assembly resolver will consider that a different assembly identity. (As it happens the assembly names changed in Rx 4.0, so not even the simple name will match.) So the assembly resolver knows it needs to find the assembly the plug-in needs. The exact rules for where it might try looking are complex, but in this case it will end up finding the copy of `System.Reactive.Core.dll` that is in the plug-in folder. This will be the copy from the NuGet package's `lib\net40` folder.

So the resolved has found an assembly that was built for .NET 4.0, but that's fine: we're running on .NET 4.8.2, but that is perfectly happy to load a .NET 4.0 component.

Now consider what happens if Plug-in A loads some time later. When it first tries to use Rx, the assembly resolver will see that it also uses `System.Reactive.Core, Version=2.2.5.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35`. It has already loaded an assembly with that exact name, so it will use that. It **won't** load the copy of `System.Reactive.Core.dll` that was in Plug-in B's folder. So Plug-in B gets the copy of this DLL from Plug-in A's folder.

And now we have a problem. Those two copies of `System.Reactive.Core.dll` are different. Plug-in B was built for the copy from the `Rx-Core` package's `lib\net45` folder, and that copy defines a few methods that are unavailable in the version in the `lib\net40` folder. (These new methods depend on functionality that was added to .NET Framework in version 4.5.) If Plug-in B attempts to use any of those methods, the CLR will throw a `MissingMethodException`, because the DLL it actually loaded was the .NET FX 4.0 version.

The basic problem here is that the .NET Framework can't load two different assemblies with the same strong name. Since Rx supplies two materially different copies of the same assembly each with the same strong name, we have a problem.

If Plug-in A had loaded first, it would have been fine: Plug-in B would have been upgraded to the .NET 4.5 version, and since that is fully backwards compatible with the .NET 4.0 version, it would have been happy.

You might think that host applications should detect this, and load plug-ins in the right order to avoid such problems. But they can't easily detect that this has happened, and also there are often good reasons not to load plug-ins until the host application knows they are required.

#### It's different on .NET (Core)

The problem just described only afflicts .NET Framework. .NET Core introduced a new mechanism that makes it possible to introduce per-plug-in assembly resolution contexts, making it possible for each plug-in to use the DLLs it supplied, even if those happen to have exactly the same names as DLLs already loaded.

.NET Core didn't appear until a few years after this problem came into existence, but I mention it because we need to ensure we don't recreate any of these old problems, so we need to take into account the changes that .NET Core (and subsequent 'modern' versions of .NET that dropped the 'Core' moniker but are part of that lineage: .NET 5.0, 6.0, etc.) introduced.



### The initial solution

Issue [#205](https://github.com/dotnet/reactive/issues/205) describes the solution adopted by Rx v3.1: it gives the DLLs in the `lib/net45` and `lib/net46` folders subtly different version numbers. (By this time (September 2016), Rx was no longer shipping versions targetting .NET 4.0, but it did offer both 4.5 and 4.6 components, so the same problem could have arisen without this fix.)

The .NET 4.5 version's full name was `System.Reactive.Core, Version=3.0.1000.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263` whereas the .NET 4.6 version's name was `System.Reactive.Core, Version=3.0.3000.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263`.

The trick here is to use the 3rd part of the assembly version number to ensure that these two different assemblies have different strong names. (These are all inside the NuGet package with version 3.1.0, so from a package perspective these are all nominally the same version. But from a .NET assembly perspective they have slightly different names.) That way in the plug-in scenario described above, it's not longer a problem if some plug-in already loaded the .NET FX 4.5 version and then a different plug-in wants the .NET FX 4.6 verison. The slight difference in the version in the assembly names means that when the assembly resolve looks for the assembly required by that newer plug-in, it does _not_ think that it already has the required assembly because the version number doesn't match. And so it goes looking for the assembly, and will load it out of the newer plug-in's folder.

So we end up with two copies of `System.Reactive.Core.dll` loaded into the application: the .NET FX 4.5 version and the .NET FX 4.6 version. (And since our host also uses Rx itself, Rx v6 will also be loaded, but as mentioned earlier, the names changed, so it will be using `System.Reactive.dll` v6.)

The solution to [#97](https://github.com/dotnet/reactive/issues/97) that [#205](https://github.com/dotnet/reactive/issues/205) describes was implemented in [PR 212](https://github.com/dotnet/reactive/pull/212).

Unfortunately, this created some new problems.


### The problems caused by the initial solution

In a [comment on #199](https://github.com/dotnet/reactive/issues/199#issuecomment-266138120), Claire Novotny listed various issues that she considered to have arisen from the initial solution ([#205](https://github.com/dotnet/reactive/issues/205)) to [#97](https://github.com/dotnet/reactive/issues/97):

* [#264](https://github.com/dotnet/reactive/issues/264): ILRepack causes issues with the PlatformEnlightenmentProvider
* [#295](https://github.com/dotnet/reactive/issues/295): System.Reactive (3.1.0) forces dependency on NETStandard.Library
* [#296](https://github.com/dotnet/reactive/issues/296): Wrong AssemblyVersion of System.Reactive.Linq in 3.1 for portable
* [#299](https://github.com/dotnet/reactive/issues/299): Version 3.1.1 has different DLL versions, depending on the platform
* [#305](https://github.com/dotnet/reactive/issues/305): Incompatibility between System.Reactive.Core and System.Reactive.Windows.Threading 3.1.1 NuGet packages

With some of these, the main issue is really something else, but the versioning strategy complicated things. But some, like [#296](https://github.com/dotnet/reactive/issues/296), are directly associated with this design.

The basic issue is that if you can end up with multiple dependency paths to what should logically be the exact same Rx component, but where those dependencies end up specifying slightly different version numbers. For example, if, say, `ExampleStd11Lib` is a .NET Standard 1.1 library that uses `System.Reactive.Core` v3.1.0, it will end up depending on `System.Reactive.Core, Version=3.0.1000.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263`. And if `ExampleNet46Lib` is a .NET FX 4.6 library that also uses `System.Reactive.Core` v3.1.0, it will end up depending on `System.Reactive.Core, Version=3.0.3000.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263`.

If we run the resulting application on .NET FX 4.6, we definitely want to use that second one. But the `ExampleStd11Lib` says it wants the first one. We can end up with both versions loaded, which can then cause baffling errors that appear to complain that some object is not of a particular type even though it clearly is. (This is because there are two identically-named versions of the same type.)

This can be handled with binding redirects: you can just configure the CLR to load the 3000 version when asked for the 1000 version, and this will ensure that both libraries are in fact using the same assembly. However, understanding the problem well enough to be able to write suitable binding redirects was a challenge, and not something we should be forcing developers to do.

This became less of a problem over time, because the build system was able to generate binding redirects for you. Even so, this is a problem you'd prefer not to have because in cases where the automated solution fails to do what you need, it is very hard to diagnose and fix.

#### It's different in .NET Core

.NET Core (and its successors, .NET 5.0, 6.0, etc.) made a very significant change in behaviour: they _won't_ load two different versions of the assembly here. They expect to load just one for any particular simple name, and consider an assembly number with a higher version than what was asked for to be acceptable.

So in the example above, if the 3.0.3000.0 version of the assembly gets loaded, the Core-etc. CLR considers that to be a perfectly acceptable resolution for the dependency on the 3.0.1000.0 version.

### The second solution

Partly due to the problems that arose from [#205](https://github.com/dotnet/reactive/issues/205)'s attempt to fix [#97](https://github.com/dotnet/reactive/issues/97), and partly because of ongoing confusion caused by the way that Rx was split across packages, Rx 4.0 unified everything into a single `System.Reactive` pacakge.

That would have been great if they'd left out the UI-framework-specific parts. Unfortunately, "everything" really did mean everything, which went on to cause some other headaches once the .NET Core versions of WPF and Windows Forms appeared. But that's a topic for other folders in this repo.


## Reproducing and Verifying Fixes

