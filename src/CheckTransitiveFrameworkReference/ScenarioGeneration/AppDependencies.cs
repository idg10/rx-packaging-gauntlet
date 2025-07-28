#pragma warning disable IDE0350 // Use implicitly typed lambda - in OneOf matches, it's typically easier to understand with explicit parameter types

namespace CheckTransitiveFrameworkReference.ScenarioGeneration;

internal record AppDependencies(
    RxDependency[] RxBefore,
    RxDependency[] RxAfter);
