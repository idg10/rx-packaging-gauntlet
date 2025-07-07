# Transitive Framework Reference Check

This Check inspects the behaviour relating to `Microsoft.WindowsDesktop.App` framework dependencies when an application has a transitive dependency on Rx.NET.

The current version of Rx.NET (6.0.1) has major problems with sometimes-unwanted `Microsoft.WindowsDesktop.App` framework dependencies. Some of the simpler proposals for fixing this fall short when transitive dependencies come into the picture, because the application author can't control the way in which Rx.NET is used, which is why we need this Check.

For an application with a transitive dependency on an older version of Rx.NET, there are three questions we want to ask about any candidate packaging approach for Rx.NET 7 in relation to this issue:

1. If the application uses self-contained deployment (perhaps via AoT), and isn't using any UI-framework-specific Rx.NET features in practice, can it avoid bloat by upgrading to a newer version of Rx.NET?
2. Suppose application is **not** using self-contained deployment (including AoT), and thus wasn't encountering the bloat problem. Also suppose that it isn't using any UI-framework-specific Rx.NET features in practice. If the developer upgrades to the latest version of Rx.NET, do packaging changes cause new problems for that scenario?
3. Suppose the application **is** using either WPF or Windows Forms Rx.NET features. In this case the 'bloat' isn't bloat: the application does actually need WPF and Windows Forms assemblies. (Because WPF and Windows Forms have interop features enabling each to host instances of the other framework's UI elements, a dependency on either effectively implies a dependency on both.) This is the scenario that `System.Reactive` v6 addresses correctly: the UI-framework-specific features you need are automatically available, and your app will acquire the necessary `Microsoft.WindowsDesktop.App` framework without needing to do so explicitly. Will upgrading to the latest Rx.NET package or packages break this, and if so, how easy will it be to make things work like they did before? (Will there be discoverability problems?)

It's important to note that although 1 has caused significant pain (leading to some projects to drop Rx.NET completely) it only affects applications using self-contained deployment (including AoT compilation), and which aren't using WPF or Windows Forms. Framework-dependent deployment doesn't suffer from the bloat problems, and  applications that are using WPF or Windows Forms really do need that framework reference. Both of these scenarios (2 and 3 above) work just fine today. So we need to be very careful to ensure that in enabling 1, we don't accidentally create a lot of new problems for scenarios 2 or 3.

Framework-dependent deployment is the default, and if we break that, we're likely to cause pain for many more people in scenario 2 than were having problems in scenario 1. Solutions that address 1 have the potential to resemble the attempts to solve the original plug-in problem in Rx 3.1: the fix worked as intended but created new problems for people who weren't writing plug-ins. And since the majority of people using Rx.NET weren't writing plug-ins, the amount of new pain caused was much greated than the amount of old pain solved.

This kind of net-more-pain outcome happens when the majority scenario is _currently_ fine, but becomes problematic as a result of a fix for a minority scenario. The fact that the majority scenario was fine before a change makes it all too easy to miss the fact that a change targeting a minority problem will make things worse in the majority scenario. The new problem isn't even on the radar because it doesn't exist yet.

For the purposes of the bloat issue, we need to keep in mind that the majority scenario is framework-dependent deployment, and the minority scenario is self-contained deployment. Ideally, any changes we make to solve the bloat problem for self-contained deployment would not create new problems for framework-dependent deployment, or for applications that actually want that framework dependency. It might not be possible to achieve this, in which case we need to consider the balance of gains and losses. We do need to be able to solve 1. The choice of solution there may be influenced by how well it deals with 2 and 3.

## Problematic Scenarios

The key aspect of this check is that we have an application using a library using some version of Rx.NET that is known to cause bloat, e.g.:

* `TheApp`
  * `SomeLibUsingRx`
    * `System.Reactive` 6.0.1

However, there are some variations on this theme.

## Consuming Application Dimensions

Before we get to the way any particular future version of Rx.NET might be packaged, there are two dimensions in this scenario creating 6 variations.

### Direct Use of Rx.NET, or Only Transitive?

Does the application also use Rx.NET itself, or does only the library use it? That is, do we have this scenario:

* `TheApp`
  * `SomeLibUsingRx`
    * `System.Reactive` 6.0.1

or this one:

* `TheApp`
  * `System.Reactive` 6.0.1
  * `SomeLibUsingRx`
    * `System.Reactive` 6.0.1

or perhaps the app was already using the latest Rx.NET, but then later acquires a dependency on a library with a dependency on an older one, in which case we'd have this scenario:

* `TheApp`
  * Proposed future Rx.NET (e.g. `System.Reactive` 7.0.0, or `System.Reactive.Net` 7.0.0, depending on the approach we take)
  * `SomeLibUsingRx`
    * `System.Reactive` 6.0.1


### Use of UI-framework-specific features in `System.Reactive`?

Does the library use any UI-framework-specific features in `System.Reactive`? That is, do we have this scenario:

* `TheApp`
  * `LibUsingOnlyNonUiFrameworkRxFeatures`
    * `System.Reactive` 6.0.1

or this:

* `TheApp`
  * `LibUsingUiFrameworkRxFeatures`
    * `System.Reactive` 6.0.1

In this second case, there's then the question of whether the application exercises any of the code paths in `LibUsingUiFrameworkRxFeatures` that use UI-frameworks-specific features. 


## Future Rx.NET Packaging Choices

As well as the dimensions created by the possible application scenario variations just described, since at the time of writing this we are still trying to decide on the packaging changes in Rx.NET 7, there are more dimensions that arise from the possible choices there:

* Is `System.Reactive` still the main component, or is it a legacy facade?
* Does `System.Reactive` continue to make the UI-framework-specific features visible at compile time, or does it relegate them to being available only at runtime in assemblies in the `lib` folder while removing them from compile-time visibility by supplying reference assemblies under `ref` with a reduced API surface area?
* Does `System.Reactive` still automatically bring in the `Microsoft.WindowsDesktop.App` framework?

The next sections describe the implications of these Rx.NET packaging choices for the application scenarios described above.

### Is `System.Reactive` still the main component or a legacy facade?

If `System.Reactive` continues to be the main component, then an application that has acquired a transitive dependency on an older version of `System.Reactive` can upgrade thus:

* `TheApp`
  * `SomeLibUsingRx`
    * `System.Reactive` 6.0.1
  * `System.Reactive` 7.0.0
  * If `SomeLibUsingRx` uses UI-framework-specific features, it's possible that one or more of the following will also be needed:
    * `UseWPF` or `UseWindowsForms` in the project file
    * References to one or more UI-framework-specific Rx.NET packages

If `System.Reactive` continues to be the main component, the application scenario where the application also uses Rx.NET directly looks exactly the same.

If `System.Reactive` is relegated to being a legacy facade, then the application scenario where Rx.NET is used only transitively, but the application upgrades to avoid bloat, looks like this:

* `TheApp`
  * `SomeLibUsingRx`
    * `System.Reactive` 6.0.1
  * `System.Reactive.Net` 7.0.0
  * `System.Reactive` 7.0.0
  * If `SomeLibUsingRx` uses UI-framework-specific features, it's possible that one or more of the following will also be needed:
    * `UseWPF` or `UseWindowsForms` in the project file
    * References to one or more UI-framework-specific Rx.NET packages



the application scenario where the application also uses Rx.NET directly looks like this:


The second scenario looks different depending on whether Rx.NET 7 continues making `System.Reactive` the main component, or relegates that to a legacy facade. In the former case, it's:

* `TheApp`
  * `LibUsingRx`
    * `System.Reactive` 6.0.1
  * `System.Reactive` 7.0.0

but in the latter case, an application might end up in this state:

* `TheApp`
  * `System.Reactive.Net` 7.0.0
  * `LibUsingRx`
    * `System.Reactive` 6.0.1

This is particularly likely if they initially didn't use `LibUsingRx` and were only using Rx.NET 7 directly. When they add `LibUsingRx`, they will find themselves in this situation where they get the older version of `System.Reactive` as a transitive dependency, and now they have references to two different Rx.NET versions. They will need to do this to unify things:

* `TheApp`
  * `System.Reactive.Net` 7.0.0
  * `System.Reactive` 7.0.0
  * `LibUsingRx`
    * `System.Reactive` 6.0.1



Issues:

* Discoverability: how does the app author know they need to upgrade to `System.Reactive` v7 as well as continuing to use `System.Reactive.Net` v7?
* Long-run: if the `LibUsingRx` does eventually upgrade to Rx.NET 7, is the app now at a disadvantage because of its ongoing `System.Reactive` dependency, and can it discover when that's no longer needed?
* 

## Command Line Behaviour

This is the first Rx Gauntlet check that needs to deal with two versions of Rx.NET at a time. Checks that use the common `RxSourceSettings` all inspect behaviour when an application is using exactly one version of Rx.NET. (This is true even for the plug-in tests: we load two plug-ins, but the failure scenarios for that were always when two plug-ins used the same Rx.NET version but different TFMs.) In those cases, we were asking whether a particular version of Rx.NET exhibits a particular behaviour.

This check is slightly different. We're not trying to establish whether given versions of Rx.NET exhibit bloat—we already have a separate check for that. Instead we want to know how potential new versions of Rx.NET when an application has a transitive dependency on an older version of Rx.NET that is already known to cause bloat.


Things to check:

* did we get any compilation errors?
* did we get any compilation warnings?
* are we expecting Rx.NET itself to emit diagnostics (e.g., hints telling you what to do), and if so did we see them?
* did we get WPF and/or Windows Forms assemblies in the output?

In cases where `System.Reactive` becomes a legacy facade, and there is a new main Rx component, we need to test two variations:

* The application adds a reference to the new main Rx component, but not to the new `System.Reactive`
* The application adds a reference to the new main Rx component, and also to the new `System.Reactive`

In cases where the main application itself is using UI framework-specific features, and if those are no longer part of the publicly visible API surface area of `System.Reactive`, we need to test what the developer sees in either of the upgrade cases just described but they have not yet added references to any of the new UI-framework-specific packages.


