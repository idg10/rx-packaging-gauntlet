using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlugIn.HostDriver;

public static class PluginHost
{
    public static Task<TResult> Run<TResult>(
        string hostRuntimeTfm,
        PluginDescriptor firstPlugin,
        PluginDescriptor secondPlugin,
        Func<Stream, Task<TResult>> stdOutStreamToResult)
    {
        throw new NotImplementedException();    
    }
}
