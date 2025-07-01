namespace RxGauntlet;

internal record TestType(string Name, string SrcFolderRelativePath)
{
    public static TestType[] All { get; } =
    [
        new TestType("Bloat (Issue 1745)", "CheckIssue1745"),
        new TestType("Extension Method Fail with DisableTransitiveFrameworkReferences", "CheckDisableTransitiveFailingExtensionMethod.csproj"),
        new TestType("Plug-in gets Wrong Rx (Issue 97)", @"Repro97\CheckIssue97"),
    ];
}
