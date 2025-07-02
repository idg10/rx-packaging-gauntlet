namespace RxGauntlet;

internal record TestType(string Name, string SrcFolderRelativePath, string ExecutableName, string OutputName)
{
    public static TestType[] All { get; } =
    [
        new TestType("Bloat (Issue 1745)", "CheckIssue1745", "CheckIssue1745.exe", "CheckIssue1745.json"),
        new TestType("Extension Method Fail with DisableTransitiveFrameworkReferences", "CheckDisableTransitiveFailingExtensionMethod", "CheckDisableTransitiveFailingExtensionMethod.exe", "CheckDisableTransitiveFailingExtensionMethod.json"),
        //new TestType("Plug-in gets Wrong Rx (Issue 97)", @"Repro97\CheckIssue97", "CheckIssue97.exe", "CheckIssue97.json"),
    ];
}
