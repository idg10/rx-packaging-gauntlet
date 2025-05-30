namespace PlugIn.HostDriver;

public record PlugInDescriptor(string TargetFrameworkMoniker, RxVersions RxVersion)
{
    // Rx 3.0
    // TODO: .NET (core)
    public static PlugInDescriptor Net45Rx30 { get; } = new PlugInDescriptor("net45", RxVersions.Rx30);
    public static PlugInDescriptor Net46Rx30 { get; } = new PlugInDescriptor("net46", RxVersions.Rx30);

    // Rx 3.1
    // TODO: .NET (core)
    public static PlugInDescriptor Net45Rx31 { get; } = new PlugInDescriptor("net45", RxVersions.Rx31);
    public static PlugInDescriptor Net46Rx31 { get; } = new PlugInDescriptor("net46", RxVersions.Rx31);

    // Rx 4.4
    public static PlugInDescriptor Net46Rx44 { get; } = new PlugInDescriptor("net46", RxVersions.Rx44);
    public static PlugInDescriptor Net462Rx44 { get; } = new PlugInDescriptor("net462", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet50Rx44 { get; } = new PlugInDescriptor("net5.0", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet50WindowsRx44 { get; } = new PlugInDescriptor("net5.0-windows", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet60Rx44 { get; } = new PlugInDescriptor("net6.0", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet60WindowsRx44 { get; } = new PlugInDescriptor("net6.0-windows", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet60Windows10019041Rx44 { get; } = new PlugInDescriptor("net6.0-windows10.0.19041", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet80Rx44 { get; } = new PlugInDescriptor("net8.0", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet80WindowsRx44 { get; } = new PlugInDescriptor("net8.0-windows", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet80Windows10019041Rx44 { get; } = new PlugInDescriptor("net8.0-windows10.0.19041", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet90Rx44 { get; } = new PlugInDescriptor("net9.0", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet90WindowsRx44 { get; } = new PlugInDescriptor("net9.0-windows", RxVersions.Rx44);
    public static PlugInDescriptor Dotnet90Windows10019041Rx44 { get; } = new PlugInDescriptor("net9.0-windows10.0.19041", RxVersions.Rx44);

    // Rx 5.0
    // Note Rx 5 requires Windows-specific TFMs to be for Windows 10 version 19041 or later.
    public static PlugInDescriptor Net462Rx50 { get; } = new PlugInDescriptor("net462", RxVersions.Rx50);
    public static PlugInDescriptor Net472Rx50 { get; } = new PlugInDescriptor("net472", RxVersions.Rx50);
    public static PlugInDescriptor Dotnet50Rx50 { get; } = new PlugInDescriptor("net5.0", RxVersions.Rx50);
    public static PlugInDescriptor Dotnet50Windows10019041Rx50 { get; } = new PlugInDescriptor("net5.0-windows10.0.19041", RxVersions.Rx50);
    public static PlugInDescriptor Dotnet60Rx50 { get; } = new PlugInDescriptor("net6.0", RxVersions.Rx50);
    public static PlugInDescriptor Dotnet60Windows10019041Rx50 { get; } = new PlugInDescriptor("net6.0-windows10.0.19041", RxVersions.Rx50);
    public static PlugInDescriptor Dotnet80Rx50 { get; } = new PlugInDescriptor("net8.0", RxVersions.Rx50);
    public static PlugInDescriptor Dotnet80Windows10019041Rx50 { get; } = new PlugInDescriptor("net8.0-windows10.0.19041", RxVersions.Rx50);
    public static PlugInDescriptor Dotnet90Rx50 { get; } = new PlugInDescriptor("net9.0", RxVersions.Rx50);
    public static PlugInDescriptor Dotnet90Windows10019041Rx50 { get; } = new PlugInDescriptor("net9.0-windows10.0.19041", RxVersions.Rx50);

    // Rx 6.0
    // TODO: .NET (core)
    public static PlugInDescriptor Net462Rx60 { get; } = new PlugInDescriptor("net462", RxVersions.Rx60);
    public static PlugInDescriptor Net472Rx60 { get; } = new PlugInDescriptor("net472", RxVersions.Rx60);

}
