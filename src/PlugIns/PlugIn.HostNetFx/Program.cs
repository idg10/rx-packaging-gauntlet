using PlugIn.Api;
using PlugIn.HostSerialization;

using System;
using System.Reflection;

namespace PlugIn.HostNetFx
{
    internal class Program
    {
#if DEBUG
        const string Configuration = "Debug";
#else
        const string Configuration = "Release";
#endif

        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: PlugIn.HostNetFx <firstPlugInDllPath> <secondPlugInDllPath>");
                return 1;
            }
            string firstPlugInPath = args[0];
            string secondPlugInPath = args[1];

            //while (!Debugger.IsAttached)
            //{
            //    Thread.Sleep(1000);
            //}

            //Debugger.Break();


            HostOutput.PlugInResult? result1 = ExecutePlugIn(firstPlugInPath);
            HostOutput.PlugInResult? result2 = ExecutePlugIn(secondPlugInPath);
            if (result1 is null || result2 is null)
            {
                return 1;
            }

            HostOutput result = new()
            {
                FirstPlugIn = result1,
                SecondPlugIn = result2
            };
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));

            return 0;
        }

        private static HostOutput.PlugInResult? ExecutePlugIn(string plugInDllPath)
        {
            Assembly plugin = Assembly.LoadFrom(plugInDllPath);
            
            Type pluginType = plugin.GetType($"PlugInTest.PlugInEntryPoint");

            object o = plugin.CreateInstance($"PlugInTest.PlugInEntryPoint");
            if (o == null)
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
    }
}
