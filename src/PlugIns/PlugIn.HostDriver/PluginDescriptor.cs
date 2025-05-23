namespace PlugIn.HostDriver;

public record PluginDescriptor(string TargetFrameworkMoniker, RxVersions RxVersion)
{
    public static PluginDescriptor Net45Rx30 { get; } = new PluginDescriptor("net45", RxVersions.Rx30);
    public static PluginDescriptor Net45Rx31 { get; } = new PluginDescriptor("net45", RxVersions.Rx31);
    public static PluginDescriptor Net46Rx30 { get; } = new PluginDescriptor("net46", RxVersions.Rx30);
    public static PluginDescriptor Net46Rx31 { get; } = new PluginDescriptor("net46", RxVersions.Rx31);
    public static PluginDescriptor Net46Rx44 { get; } = new PluginDescriptor("net46", RxVersions.Rx44);
    public static PluginDescriptor Net462Rx44 { get; } = new PluginDescriptor("net462", RxVersions.Rx44);
    public static PluginDescriptor Net462Rx50 { get; } = new PluginDescriptor("net462", RxVersions.Rx50);
    public static PluginDescriptor Net462Rx60 { get; } = new PluginDescriptor("net462", RxVersions.Rx60);
    public static PluginDescriptor Net472Rx50 { get; } = new PluginDescriptor("net472", RxVersions.Rx50);
    public static PluginDescriptor Net472Rx60 { get; } = new PluginDescriptor("net472", RxVersions.Rx60);
}
