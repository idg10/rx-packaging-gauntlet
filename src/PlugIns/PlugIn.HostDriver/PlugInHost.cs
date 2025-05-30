using System.Diagnostics;

namespace PlugIn.HostDriver;

public static class PlugInHost
{
#if DEBUG
    const string Configuration = "Debug";
#else
        const string Configuration = "Release";
#endif

    public async static Task<TResult> Run<TResult>(
        string hostRuntimeTfm,
        PlugInDescriptor firstPlugIn,
        PlugInDescriptor secondPlugIn,
        Func<Stream, Task<TResult>> stdOutStreamToResult)
    {
        string launcher;

        if (hostRuntimeTfm.StartsWith("net"))
        {
            if (hostRuntimeTfm.Contains("."))
            {
                // .NET Core or .NET 5+
                launcher = "PlugIn.HostDotnet";
            }
            else
            {
                // .NET Framework
                launcher = "PlugIn.HostNetFx";
            }
        }
        else
        {
            throw new ArgumentException($"Unsupported host runtime TFM: {hostRuntimeTfm}");
        }

        DirectoryInfo plugInsFolder = new DirectoryInfo(
            Path.Combine(AppContext.BaseDirectory, "../../../../../PlugIns/"));
        if (!plugInsFolder.Exists)
        {
            throw new DirectoryNotFoundException($"PlugIns folder not found: {plugInsFolder.FullName}");
        }

        string plugInHostProjectFolder = Path.Combine(
            plugInsFolder.FullName,
            launcher);
        if (!Directory.Exists(plugInHostProjectFolder))
        {
            throw new DirectoryNotFoundException($"PlugIn host project folder not found at {plugInHostProjectFolder}");
        }
        string plugInHostExecutableFolder = Path.Combine(
            plugInHostProjectFolder,
            $"bin/{Configuration}/{hostRuntimeTfm}/");
        if (!Directory.Exists(plugInHostExecutableFolder))
        {
            throw new DirectoryNotFoundException($"PlugIn host build output folder not found at {plugInHostExecutableFolder}");
        }

        string plugInHostExecutablePath = Path.Combine(
            plugInHostExecutableFolder,
            $"{launcher}.exe");
        if (!File.Exists(plugInHostExecutablePath))
        {
            throw new FileNotFoundException($"PlugIn host executable not found at {plugInHostExecutablePath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = plugInHostExecutablePath,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = $"{firstPlugIn.RxVersion} {firstPlugIn.TargetFrameworkMoniker} {secondPlugIn.RxVersion} {secondPlugIn.TargetFrameworkMoniker}",
            WorkingDirectory = plugInHostExecutableFolder,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Pass the StandardOutput stream to the provided function
        Task<TResult> resultTask = stdOutStreamToResult(process.StandardOutput.BaseStream);
        Task processTask = process.WaitForExitAsync();
        Task firstToFinish = await Task.WhenAny(processTask, resultTask);

        if (!resultTask.IsCompleted)
        {
            // The process finished, but the result task is still running. It's possible that
            // it is nearly done, so give it some time.
            await Task.WhenAny(resultTask, Task.Delay(2000));
        }

        if (!resultTask.IsCompleted)
        {
            throw new InvalidOperationException("Did not get output from program");
        }
        var result = await resultTask;

        return result;
    }

    private static string GetPlugInArgument(PlugInDescriptor descriptor)
    {
        bool isNetFx = descriptor.TargetFrameworkMoniker.StartsWith("net") && !descriptor.TargetFrameworkMoniker.Contains('.');
        string frameworkNamePart = isNetFx ? "NetFx" : "Dotnet";
        return $"PlugIn.{frameworkNamePart}.{descriptor.TargetFrameworkMoniker}.{descriptor.RxVersion}";
    }
}
