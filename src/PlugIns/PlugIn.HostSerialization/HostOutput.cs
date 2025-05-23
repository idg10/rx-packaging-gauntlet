using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlugIn.HostSerialization;

public class HostOutput
{
    public PlugInResult FirstPlugIn { get; set; }
    public PlugInResult SecondPlugIn { get; set; }

    public class PlugInResult
    {
        public string RxFullAssemblyName { get; set; }
        public string RxLocation { get; set; }
        public string RxTargetFramework { get; set; }
    }
}