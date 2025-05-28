namespace PlugIn.HostDriver;

public record PlugInDescriptor(string TargetFrameworkMoniker, RxVersions RxVersion)
{
    public static PlugInDescriptor Net45Rx30 { get; } = new PlugInDescriptor("net45", RxVersions.Rx30);
    public static PlugInDescriptor Net45Rx31 { get; } = new PlugInDescriptor("net45", RxVersions.Rx31);
    public static PlugInDescriptor Net46Rx30 { get; } = new PlugInDescriptor("net46", RxVersions.Rx30);
    public static PlugInDescriptor Net46Rx31 { get; } = new PlugInDescriptor("net46", RxVersions.Rx31);
    public static PlugInDescriptor Net46Rx44 { get; } = new PlugInDescriptor("net46", RxVersions.Rx44);
    public static PlugInDescriptor Net462Rx44 { get; } = new PlugInDescriptor("net462", RxVersions.Rx44);
    public static PlugInDescriptor Net462Rx50 { get; } = new PlugInDescriptor("net462", RxVersions.Rx50);
    public static PlugInDescriptor Net462Rx60 { get; } = new PlugInDescriptor("net462", RxVersions.Rx60);
    public static PlugInDescriptor Net472Rx50 { get; } = new PlugInDescriptor("net472", RxVersions.Rx50);
    public static PlugInDescriptor Net472Rx60 { get; } = new PlugInDescriptor("net472", RxVersions.Rx60);
}
