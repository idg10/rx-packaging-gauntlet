namespace PlugIn.Api;

public interface IRxPlugInApi
{
    public string GetPlugInAssemblyLocation();

    public string GetRxFullName();
    public string GetRxLocation();
    public string GetRxTargetFramework();

    public RxCancellationFlowBehaviour GetRxCancellationFlowBehaviour();

    bool IsWindowsFormsSupportAvailable();
}
