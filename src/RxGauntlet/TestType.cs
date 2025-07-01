namespace RxGauntlet;

internal record TestType(string Name, string SrcFolderRelativePath, string ExecutableName)
{
    public static TestType[] All { get; } =
    [
        new TestType("Bloat (Issue 1745)", "CheckIssue1745", "CheckIssue1745.exe"),
        new TestType("Extension Method Fail with DisableTransitiveFrameworkReferences", "CheckDisableTransitiveFailingExtensionMethod.csproj", "CheckDisableTransitiveFailingExtensionMethod.exe"),
        new TestType("Plug-in gets Wrong Rx (Issue 97)", @"Repro97\CheckIssue97", "CheckIssue97.exe"),
    ];
}
