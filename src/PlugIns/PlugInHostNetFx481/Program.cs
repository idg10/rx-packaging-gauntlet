using System;
using System.Reflection;

namespace PlugInHostNetFx481
{
    internal class Program
    {
#if DEBUG
        const string Configuration = "Debug";
#else
        const string Configuration = "Release";
#endif

        private enum BehaviourComparison
        {
            // The original plug-in conflict problem that Rx 3.1 addressed was when two plug-ins
            // were both built against the same version of Rx, but targetted .NET Framework 4.5
            // and 4.6. Because Rx 3.0 provided different DLLs for .NET 4.5 and .NET 4.6, the
            // plug-ins would disagree about which Rx DLLs to use.
            // To verify that we are able to correctly reproduce the scenario that Rx 3.1 fixed
            // with https://github.com/dotnet/reactive/issues/205 we need to be able to load two
            // plug-ins, one targetting .NET 4.5 and the other targetting .NET 4.6, each targetting
            // the same version of Rx.
            // So when this comparison is selected, we expect to see the plug-in load order change
            // the behaviour with Rx 3.0 but not with Rx 3.1.
            // This comparison is available only for Rx 3.0 and 3.1 because later versions of Rx
            // did not offet a net45 target.
            Net45ToNet46,

            // Rx v4.X (all versions) offered .NET Standard 2.0 and .NET 4.6 DLLs.
            // .NET 4.6 does not support .NET Standard 2.0, but .NET 4.6.2 and later do, so you
            // might expect a plug-in targetting .NET 4.6.2 that uses Rx 4.4.1 to get the
            // netstandard2.0 System.Reactive assembly, because there's a sense in which
            // .NET Standard 2.0 is newer than .NET 4.6. However, the .NET build system
            // considers net46 to be a better match for all .NET Frameworks >= 4.6, so in practice
            // the only DLL in any Rx 4.X package that will get loaded into a .NET Framework
            // application is the net46 one.
            // So if you have a .NET 4.6 and a .NET 4.6.2 plug-in both using Rx 4.4.1, they should
            // both want the net46 System.Reactive assembly, and there should be no conflict.
            // This comparison mode verifies that this is indeed the case. It is applicable only
            // to Rx 4.X because earlier versions of Rx did not offer a netstandard2.0 target, and
            // later versions of Rx don't have a .NET Framework target older than 4.7.2, meaning
            // that plug-ins targetting either .NET 4.6 or .NET 4.6.2 would both use the
            // netstandard2.0 DLL.
            Net46To462,

            // Rx 5.0 reintroduced the problem in which plug-ins might have conflicting ideas about
            // which particular DLL to use from a specific Rx version. This was caused by two
            // changes:
            //  1. Rx 4.0 dropped the versioning scheme introduced in Rx 3.1
            //  2. Rx 5.0 changed its .NET Framework target from net46 to net472
            // On its own, 1 was not enough to cause the problem, Rx 4.X offered only one DLL that
            // would ever be a candidate for use by a .NET Framework project, the net46 one. But
            // by changing the target to net472, Rx 5.0 meant that the following was true:
            //  * A plug-in targetting .NET 4.6.2 would want the netstandard2.0 DLL
            //  * A plug-in targetting .NET 4.7.2 would want the net472 DLL
            // Since it would be possible for a single plug-in host application to load a plug-in
            // of each kind, it becomes possible for the plug-ins to disagree about which DLL to
            // load. For example, the plug-in built for .NET 4.7.2 might try to use the WPF support
            // built into the net472 System.Reactive assembly, but if the .NET 4.6.2 plug-in had
            // already loaded the netstandard2.0 version of System.Reactive, the .NET 4.7.2 plug-in
            // would get either a TypeLoadException or a FileNotFoundException when it tried to use
            // the WPF features.
            // This comparison mode lets us verify that Rx 5.0 really did cause this regression,
            // and also to verify that it continues to be the case for later versions of Rx.
            NetStandardToNet472
        }

        [STAThread]
        static void Main(string[] args)
        {
            bool? oldFirstN = (args.Length == 2 ? args[0] : null) switch
            {
                "oldfirst" => true,
                "oldlast" => false,
                _ => null
            };
            string rxVersion = (args.Length == 2 ? args[1] : null);
            BehaviourComparison? behaviourComparisonN = rxVersion switch
            {
                "30" => BehaviourComparison.Net45ToNet46,
                "31" => BehaviourComparison.Net45ToNet46,
                "44" => BehaviourComparison.Net46To462,
                "50" => BehaviourComparison.NetStandardToNet472,
                "60" => BehaviourComparison.NetStandardToNet472,
                _ => throw new ArgumentException("Invalid Rx version")
            };

            if (oldFirstN is not bool oldFirst ||
                behaviourComparisonN is not BehaviourComparison behaviourComparison)
            {
                Console.WriteLine("Arguments required: [oldfirst|oldlast] [30|31|50|60]");
                return;
            }

            (string oldFxName, string newFxName) = behaviourComparison switch
            {
                BehaviourComparison.Net45ToNet46 => (".NET 4.5", ".NET 4.6"),
                BehaviourComparison.Net46To462 => (".NET 4.6", ".NET 4.6.2"),
                BehaviourComparison.NetStandardToNet472 => (".NET Standard 2.0", ".NET 4.7.2"),
                _ => throw new ArgumentException("Invalid behaviour comparison")
            };
            (string falseResultDescription, string trueResultDescription) = behaviourComparison switch
            {
                BehaviourComparison.Net45ToNet46 => (".NET 4.5 (Cancellation not flowed)", ".NET 4.6 (Cancellation flowed)"),
                BehaviourComparison.Net46To462 => ("UI support unavailable", "UI support available"),
                BehaviourComparison.NetStandardToNet472 => ("UI support unavailable", "UI support available"),
                _ => throw new ArgumentException("Invalid behaviour comparison")
            };

            (Func<bool> runOld, Func<bool> runNew) = behaviourComparison switch
            {
                BehaviourComparison.Net45ToNet46 =>
                    ((Func<bool> runOld, Func<bool> runNew))( // Cast required on at least one case so compiler can infer target type of switch
                    () => TestForNet46Behaviour("45", rxVersion),
                    () => TestForNet46Behaviour("46", rxVersion)),

                BehaviourComparison.Net46To462 => (
                    // Note: to load the .NET Standard 2.0 version of Rx.NET, we build for .NET 4.6.2
                    () => TestForBehaviour("46", rxVersion, "AreWindowsFormsTypesAvailable"),
                    () => TestForBehaviour("462", rxVersion, "AreWindowsFormsTypesAvailable")),

                BehaviourComparison.NetStandardToNet472 => (
                    // Note: to load the .NET Standard 2.0 version of Rx.NET, we build for .NET 4.6.2
                    () => TestForBehaviour("462", rxVersion, "AreWindowsFormsTypesAvailable"),
                    () => TestForBehaviour("472", rxVersion, "AreWindowsFormsTypesAvailable")),

                _ => throw new InvalidOperationException($"Unhandled comparison type: {behaviourComparison}")
            };  

            string Behaviour(bool newBehaviour) => newBehaviour ? trueResultDescription : falseResultDescription;
            string Behaviours((bool, bool) b) => $"({oldFxName}: {Behaviour(b.Item1)}, {newFxName}: {Behaviour(b.Item2)})";

            (bool oldBehaviour, bool newBehaviour) actualBehaviour;
            (bool, bool) expectedBehaviour;

            if (oldFirst)
            {
                Console.WriteLine($"Loading {oldFxName} plug-in first, then {newFxName} plug-in");

                actualBehaviour.oldBehaviour = runOld();
                actualBehaviour.newBehaviour = runNew();

                expectedBehaviour = rxVersion switch
                {
                    // Rx 3.0 known bug:
                    "30" => (false, false),

                    "31" => (false, true),
                    "44" => (true, true),

                    // Rx 5.0/6.0 known bug:
                    "50" => (false, false),
                    "60" => (false, false),
                    _ => throw new ArgumentException("Invalid Rx version")
                };
            }
            else
            {
                Console.WriteLine($"Loading {newFxName} plug-in first, then {oldFxName} plug-in");

                actualBehaviour.newBehaviour = runNew();
                actualBehaviour.oldBehaviour = runOld();

                expectedBehaviour = rxVersion switch
                {
                    // Rx 3.0 known bug:
                    "30" => (true, true),

                    "31" => (false, true),
                    "44" => (true, true),

                    // Rx 5.0/6.0 known bug:
                    "50" => (true, true),
                    "60" => (true, true),
                    _ => throw new ArgumentException("Invalid Rx version")
                };
            }

            if (actualBehaviour == expectedBehaviour)
            {
                Console.WriteLine($"Both plug-ins saw expected behaviour for Rx {rxVersion}: {Behaviours(actualBehaviour)}");
            }
            else
            {
                Console.WriteLine($"Expected behaviour: {Behaviours(expectedBehaviour)}");
                Console.WriteLine($"Actual behaviour: {Behaviours(actualBehaviour)}");
            }
        }

        private static bool TestForNet46Behaviour(string netVersion, string rxVersion)
        {
            return TestForBehaviour(netVersion, rxVersion, "RxExhibitsNet46Behaviour");
        }

        private static bool TestForBehaviour(string netVersion, string rxVersion, string methodName)
        {
            Assembly plugin = Assembly.LoadFrom($@"..\..\..\..\PlugInNet{netVersion}Rx{rxVersion}\bin\{Configuration}\net{netVersion}\PlugInNet{netVersion}Rx{rxVersion}.dll");
            Type pluginType = plugin.GetType($"PlugInTest.PlugInEntryPoint");
            MethodInfo method = pluginType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            var result = (bool)method.Invoke(null, null);

            return result;
        }
    }
}
