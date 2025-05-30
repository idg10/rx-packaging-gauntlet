using PlugIn.Api;
using PlugIn.HostSerialization;

using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PlugInHostNetFx481
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
            if (args.Length != 4)
            {
                Console.Error.WriteLine("Usage: PlugIn.HostNetFx firstPlugInRxVersion firstPlugInTfm secondPlugInRxVersion secondPlugInTfm");
                Console.Error.WriteLine("E.g.: PlugIn.HostNetFx Rx44 net6.0 Rx44 net8.0");
                return 1;
            }
            string firstPlugInRxVersion = args[0];
            string firstPlugInTfm = args[1];
            string secondPlugInRxVersion = args[2];
            string secondPlugInTfm = args[3];

            //while (!Debugger.IsAttached)
            //{
            //    Thread.Sleep(1000);
            //}

            //Debugger.Break();


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
        }

        private static HostOutput.PlugInResult? ExecutePlugIn(string rxVersion, string tfm)
        {
            string plugInName = $"PlugIn.NetFx.{tfm}.{rxVersion}";
            Assembly plugin = Assembly.LoadFrom($@"..\..\..\..\{plugInName}\bin\{Configuration}\{tfm}\{plugInName}.dll");
            
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
