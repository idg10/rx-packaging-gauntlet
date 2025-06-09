using PlugIn.Api;
using PlugIn.HostSerialization;

using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

if (args is not [string firstPlugInRxVersion, string firstPlugInTfm, string secondPlugInRxVersion, string secondPlugInTfm])
{
    Console.Error.WriteLine("Usage: PlugIn.HostDotnet firstPlugInRxVersion firstPlugInTfm secondPlugInRxVersion secondPlugInTfm");
    Console.Error.WriteLine("E.g.: PlugIn.HostDotnet Rx44 net462 Rx44 net472");
    return 1;
}

HostOutput.PlugInResult? result1 = ExecutePlugIn(firstPlugInRxVersion, firstPlugInTfm);
HostOutput.PlugInResult? result2 = ExecutePlugIn(secondPlugInRxVersion, secondPlugInTfm);
if (result1 is null || result2 is null)
{
    return 1;
}

HostOutput result = new HostOutput
{
    FirstPlugIn = result1,
    SecondPlugIn = result2
};
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));

return 0;

static HostOutput.PlugInResult? ExecutePlugIn(string rxVersion, string tfm)
{
#if DEBUG
    const string Configuration = "Debug";
#else
        const string Configuration = "Release";
#endif

    string plugInName = $"PlugIn.Dotnet.{rxVersion}";
    string plugInDllPath = Path.GetFullPath($@"..\..\..\..\{plugInName}\bin\{Configuration}\{tfm}\{plugInName}.dll");
    PlugInLoadContext plugInLoadContext = new(plugInDllPath);
    Assembly plugin = plugInLoadContext.LoadFromAssemblyPath(plugInDllPath);

    if (plugin.GetType($"PlugInTest.PlugInEntryPoint") is not Type pluginType)
    {        
        Console.Error.WriteLine("PlugInTest.PlugInEntryPoint type not found.");
        return null;
    }

    if (plugin.CreateInstance($"PlugInTest.PlugInEntryPoint") is not object o)
    {
        Console.Error.WriteLine($"Failed to create instance of {pluginType.FullName}");
        return null;
    }

    if (o is not IRxPlugInApi instance)
    {
        Console.Error.WriteLine($"Plug-in does not implement {nameof(IRxPlugInApi)}");
        return null;
    }

    return new HostOutput.PlugInResult
    {
        PlugInLocation = instance.GetPlugInAssemblyLocation(),

        RxFullAssemblyName = instance.GetRxFullName(),
        RxLocation = instance.GetRxLocation(),
        RxTargetFramework = instance.GetRxTargetFramework(),

        FlowsCancellationTokenToOperationCancelledException =
            instance.GetRxCancellationFlowBehaviour() == RxCancellationFlowBehaviour.FlowedToOperationCanceledException,
        SupportsWindowsForms = instance.IsWindowsFormsSupportAvailable()
    };
}


internal class PlugInLoadContext(string plugInPath) : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver = new(plugInPath);

    protected override Assembly Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name != typeof(IRxPlugInApi).Assembly.GetName().Name)
        {
            if (_resolver.ResolveAssemblyToPath(assemblyName) is string assemblyPath)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
        }

        return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
    }
}