using PlugIn.Api;
using System.Reflection;
using System.Reactive.Linq;

namespace PlugInTest;

// Note: this file is compiled into all of the plug-ins:
//  * the .NET 4.5 and .NET 4.6 plug-ins
//  * all versions of Rx.NET
public partial class PlugInEntryPoint : IRxPluginApi
{
    public string GetRxFullName() => typeof(Observable).Assembly.FullName;
    public string GetRxLocation() => typeof(Observable).Assembly.Location;
    public string GetRxTargetFramework() => typeof(Observable).Assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>().FrameworkName;
}
