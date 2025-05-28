namespace PlugIn.Api;

public interface IRxPluginApi
{
    public string GetPlugInAssemblyLocation();

    public string GetRxFullName();
    public string GetRxLocation();
    public string GetRxTargetFramework();

    public RxCancellationFlowBehaviour GetRxCancellationFlowBehaviour();

    bool IsWindowsFormsSupportAvailable();
}
