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
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: PlugIn.HostNetFx firstPlugIn secondPlugIn");
                return 1;
            }
            string firstPlugIn = args[0];
            string secondPlugIn = args[1];

            //while (!Debugger.IsAttached)
            //{
            //    Thread.Sleep(1000);
            //}

            //Debugger.Break();


            HostOutput.PlugInResult? result1 = ExecutePlugIn(firstPlugIn);
            HostOutput.PlugInResult? result2 = ExecutePlugIn(secondPlugIn);
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

        private static HostOutput.PlugInResult? ExecutePlugIn(string plugInName)
        {
            Match re = Regex.Match(plugInName, @"PlugIn\.(?<Runtime>[^.]+)\.(?<Tfm>[^.]+)\.Rx(?<RxVersion>\d+)");
            string runtime = re.Groups["Runtime"].Value;
            string tfm = re.Groups["Tfm"].Value;
            string rxVersion = re.Groups["RxVersion"].Value;

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

            string plugInLocation = instance.GetPlugInAssemblyLocation();

            string rxFullName = instance.GetRxFullName();
            string rxLocation = instance.GetRxLocation();
            string rxTargetFramework = instance.GetRxTargetFramework();

            return new HostOutput.PlugInResult
            {
                PlugInLocation = plugInLocation,

                RxFullAssemblyName = rxFullName,
                RxLocation = rxLocation,
                RxTargetFramework = rxTargetFramework
            };
        }
    }
}
