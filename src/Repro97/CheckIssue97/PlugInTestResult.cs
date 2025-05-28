using PlugIn.HostDriver;
using PlugIn.HostSerialization;

namespace CheckIssue97;

public record PlugInTestResult(
    string HostTargetFrameworkMoniker,
    PluginDescriptor PlugIn1,
    PluginDescriptor PlugIn2,
    HostOutput Result);
