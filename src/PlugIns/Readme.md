# Plug-Ins

Rx.NET versions up to and including v3.0 could encounter a problem with plug-in systems. Two plug-ins
hosted in the same application could both depend on the same version of Rx.NET, but could disagree about which
actual Rx.NET DLLs to load. Since .NET Framework is not capable of allowing two different plug-ins to load
two different copies of a single DLL when both have the same strong name, one of the plug-ins would not get
the DLL it was expecting. This could result in runtime failures.

Although this problem was fixed in Rx .NET v3.1, there was a regression in Rx .NET v5.0, and the problem
continues to be present in Rx .NET v6.0.


## Problem Detail

This situation could occur with
[Rx.NET 3.0.0](https://www.nuget.org/packages/System.Reactive.Core/3.0.0#supportedframeworks-body-tab)
because it offered both `net45` and `net46` targets, each of which had exactly the same strong name. A
a single .NET 4.6 host application could load two different plug-ins that both used Rx 3.0.0, but where
one plug-in was built for .NET 4.5 and the other for .NET 4.6.

Plug-ins include copies of all the DLLs they require. NuGet package resolution is done when the plug-in is
built, so at runtime, the host application has no idea of where any of the DLLs came from. This means that
the host application itself can't apply the version resolution logic of the kind that normally occurs with
NuGet packages.

If the .NET 4.5 plug-in loads first, it will be the first code in the process to try to load
`System.Reactive.Core` v3.0.0. The .NET assembly loader will load the copy of that DLL that the plug-in has
supplied. If the .NET 4.6 plug-in loads later, it will also depend on `System.Reactive.Core` v3.0.0, and will
bring its own copy. That copy will be different—it will be a copy of the one that was in the NuGet package's
`net46` folder, whereas the .NET 4.5 plug-in will have copied its `System.Reactive.Core.dll` from the `net45`
folder.

In essence, .NET Framework plug-in systems contravene a basic assumption of NuGet: that it will be possible
to get a complete overview of all the components required, and to apply a resolution process to pick the
specific DLLs that will be used. (.NET Core and subsequence .NET versions don't have this problem because
the `AssemblyLoadContext` makes it possible for each plug-in to load whatever version of a DLL it wants,
regardless of what other plug-ins may have done.)


## The fix in Rx 3.1

[Pull Request #212](https://github.com/dotnet/reactive/pull/212) implemented the fix described in
[Issue #205](https://github.com/dotnet/reactive/issues/205). This gave each distinct DLL within a single
NuGet package a slightly different version number. This made it possible to load the .NET 4.5 and the
.NET 4.6 DLL for Rx 3.1.0 simultaneously, because they had different strong names.


## How The Great Unification Paved the Way for a Regression

Rx 4.0 saw the "Great Unification" in which all the Rx.NET packages were merged into a single package.
At the same time, the version number hack that had been applied in Rx 3.1 did not continue with the
`System.Reactive.dll` files in Rx 4.0. (The old separate packages continue to this day to have the
version numbers that were applied in v3.1, but those packages are now just type forwarders providing
backwards compatibility with old code built for Rx 3.1.)

The decision to drop the version number hack doesn't appear to have been discussed in detail anywhere
in GitHub (as far we've been able to tell). We think that the prevailing view was that the hack was
no longer necessary, because the unification rendered it unnecessarily. However, if that was indeed
the view, it was an oversimplification. In fact, the main reason the Rx-3.1-style hack was no longer
necessary was that Rx 4.0 happened to have exactly one TFM that would be used on any .NET Framework
target.

Superficially that might not look true: it offers both `net46` and `netstandard2.0` and the latter
could in theory be used on .NET Framework 4.6.2 and later. However, the .NET build tools consider the
`net46` target to be a better match for any .NET Framework version from 4.6 onwards than `netstandard2.0`,
and `netstandard2.0` is not supported on any version of .NET Framework older than 4.6.2. So on all
versions of .NET Framework in existence that are new enough to be able to how Rx 4.0 at all, the `net46`
target would be selected.

For as long as that was true, the Rx 3.1 era hack was not necessary.

## The Regression in Rx 5.0

Rx 5.0 dropped the `net46` target, and added a `net472` target. Unfortunately, that meant that the
plug-in version problem was back. This is because there are, once again, two TFMs in Rx which are
applicable to different versions of .NET Framework.

If you build for .NET Framework 4.6.2, 4.7, or 4.7.1, you will get the `netstandard2.0` target. (The
`net472` target will not be available on these older versions. But those framework versions _do_ support
.NET Standard 2.0.) And if you build for .NET Framework 4.7.2 or later, you will get the `net472` target.

This means we're back in a new version of the situation that existed for Rx 3.0: two plug-ins could
target the same version of Rx.NET, but could disagree about which actual DLLs to load.

This situation continues to exist in current Rx.NET versions. (Again, it only affects .NET Framework,
because that does not have the `AssemblyLoadContext` that enables .NET plug-in hosts to avoid this problem.)


## Demonstrating the Problem

To be able to determine whether future versions of Rx.NET will have the same problems, we need a reliable
way to reproduce the issue. This folder contains a [.NET Framework 4.8.1 console application](./PlugInHostNetFx481)
that acts as a plug-in host, and a large number of projects building plug-ins targetting various versions
of .NET Framework, and with dependencies on various versions of Rx.NET.

The [`Common`](./Common) folder contains source files that can distinguish between versions of Rx.NET
at runtime by checking for certain behaviours:

* [`PlugInEntryPoint.Net46Behaviour.cs`](./Common/PlugInEntryPoint.Net46Behaviour.cs) can determine whether the version of Rx.NET available has the behaviour that was added in the `net46` target of Rx.NET 3.0.0. We use this when reproducing the problem on Rx 3.0, because it lets us work out whether the `net45` or `net46` DLL was loaded.
* [`PlugInEntryPoint.WindowsFormsAvailable`](./Common/PlugInEntryPoint.WindowsFormsAvailable.cs) can determine whether Windows Forms support is available. We use this for Rx 4.0 and later (because that's when UI framework support got bundled into the main `System.Reactive` component), and it effectively lets us determine whether the .NET Framework DLL (`net46` or `net472`) got loaded (in which case UI features will be present) or we ended up with the `netstandard2.0` version.

The host application takes a couple of command line arguments that determine:

* Which version of Rx.NET to test (3.0, 3.1, 4.4, 5.0, or 6.0)
  * With 3.0 and 3.1, we the tool will determine whether the `net45` or `net46` version of Rx.NET was loaded.
  * With all other targets we determine whether the .NET Framework or .NET Standard version of Rx.NET was loaded.
* Whether to load the plug-in built for a lower version of .NET first or second

In each case the application is pre-configured with the expected behaviour (where 'expected' includes known bugs, such as the
problematic behaviour in Rx 3.0, and in Rx >= 5.0) and reports whether the observed behaviour matches the expected behaviour.
We expect it to report that the behaviour matches in all cases, because the purpose of this is to confirm that we know
how to trigger repros. As future versions of Rx.NET are under development, we can add new configurations to this tool
that check that the behaviour for these is what we plan.