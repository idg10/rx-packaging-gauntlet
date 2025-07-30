// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

namespace RxGauntlet;

internal record TestType(string Name, string SrcFolderRelativePath, string ExecutableName, string OutputName)
{
    public static TestType[] All { get; } =
    [
        new TestType("Bloat (Issue 1745)", @"Checks\Bloat\CheckIssue1745", "CheckIssue1745.exe", "CheckIssue1745.json"),
        new TestType("Extension Method Fail with DisableTransitiveFrameworkReferences", @"Checks\ExtensionMethods\CheckDisableTransitiveFailingExtensionMethod", "CheckDisableTransitiveFailingExtensionMethod.exe", "CheckDisableTransitiveFailingExtensionMethod.json"),
        new TestType("Plug-in gets Wrong Rx (Issue 97)", @"Checks\PlugIns\CheckIssue97", "CheckIssue97.exe", "CheckIssue97.json"),
        new TestType("Transitive References", @"Checks\TransitiveReferences\CheckTransitiveFrameworkReference", "CheckTransitiveFrameworkReference.exe", "CheckTransitiveFrameworkReference.json"),
    ];
}
