using PlugIn.HostDriver;
using PlugIn.HostSerialization;

namespace CheckIssue97;

public record PlugInTestResult(
    string HostTargetFrameworkMoniker,
    PlugInDescriptor PlugIn1,
    PlugInDescriptor PlugIn2,
    HostOutput Result);
