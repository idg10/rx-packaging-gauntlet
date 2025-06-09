using PlugIn.Api;

namespace PlugInTest;

public partial class PlugInEntryPoint : IRxPlugInApi
{
    public string GetPlugInAssemblyLocation()
    {
        return typeof(PlugInEntryPoint).Assembly.Location;
    }
}