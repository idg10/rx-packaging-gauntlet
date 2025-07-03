using PlugIn.HostDriver;

namespace CheckIssue97;

internal record Scenario(string HostTfm, PlugInDescriptor firstPlugIn, PlugInDescriptor secondPlugIn);
