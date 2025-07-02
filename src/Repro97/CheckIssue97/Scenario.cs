using PlugIn.HostDriver;

namespace CheckIssue97;

internal record Scenario(PlugInDescriptor firstPlugIn, PlugInDescriptor secondPlugIn);
