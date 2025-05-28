using PlugIn.Api;

namespace PlugInTest;

public partial class PlugInEntryPoint : IRxPluginApi
{
    public string GetPlugInAssemblyLocation()
    {
        return typeof(PlugInEntryPoint).Assembly.Location;
    }
}