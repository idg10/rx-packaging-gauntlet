namespace PlugIn.HostSerialization;

public class HostOutput
{
    public PlugInResult FirstPlugIn { get; set; }
    public PlugInResult SecondPlugIn { get; set; }

    public class PlugInResult
    {
        public string PlugInLocation { get; set; }
     
        public string RxFullAssemblyName { get; set; }
        public string RxLocation { get; set; }
        public string RxTargetFramework { get; set; }
    }
}