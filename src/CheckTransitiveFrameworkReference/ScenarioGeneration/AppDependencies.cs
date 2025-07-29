// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0350 // Use implicitly typed lambda - in OneOf matches, it's typically easier to understand with explicit parameter types

namespace CheckTransitiveFrameworkReference.ScenarioGeneration;

internal record AppDependencies(
    RxDependency[] RxBefore,
    RxDependency[] RxAfter);
