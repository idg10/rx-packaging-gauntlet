// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace RxGauntlet.Build;

public sealed class ModifiedProjectClone : IDisposable
{
    private readonly string _copyPath;

    private ModifiedProjectClone(string copyPath)
    {
        _copyPath = copyPath;
    }

    public string ClonedProjectFolderPath => _copyPath;

    public static ModifiedProjectClone Create(
        string sourceProjectFolder,
        string copyParentFolderName,
        Action<ProjectFileRewriter> modifyProjectFile,
        (string FeedName, string FeedLocation)[]? additionalPackageSources)
    {
        var copyPath = Path.Combine(
            Path.GetTempPath(),
            "RxGauntlet",
            copyParentFolderName,
            DateTime.Now.ToString("yyyyMMdd-HHmmss"));

        Directory.CreateDirectory(copyPath);

        ModifiedProjectClone? clone = new(copyPath);
        try
        {
            foreach (var file in Directory.GetFiles(sourceProjectFolder))
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                var relativePath = Path.GetRelativePath(sourceProjectFolder, file);
                var destinationPath = Path.Combine(copyPath, relativePath);

                switch (extension)
                {
                    case ".cs":
                        File.Copy(file, destinationPath, true);
                        break;

                    case ".csproj":
                        var projectFileRewriter = ProjectFileRewriter.CreateForCsProj(file);
                        modifyProjectFile(projectFileRewriter);
                        projectFileRewriter.WriteModified(destinationPath);
                        break;
                }
            }

            if (additionalPackageSources is not null && additionalPackageSources.Length > 0)
            {
                // We need to emit a NuGet.config file, because the arguments specified one or more custom package sources
                var sources = string.Join(Environment.NewLine, additionalPackageSources.Select(
                    p => $"""    <add key="{p.FeedName}" value="{p.FeedLocation}" />"""));
                var nuGetConfigContent = $"""
                            <?xml version="1.0" encoding="utf-8"?>
                            <configuration>
                              <packageSources>
                                <clear />
                                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                            {sources}
                              </packageSources>
                            </configuration>
                            """;

                File.WriteAllText(
                    Path.Combine(copyPath, "NuGet.config"),
                    nuGetConfigContent);
            }

            // We're now going to return without error, so we no longer want the finally block
            // to delete the directory. That will now happen when the caller calls Dispose on
            // the ModifiedProjectClone we return..
            var result = clone;
            clone = null;
            return result; 
        }
        finally
        {
            if (clone is not null)
            {
                // If we reach here, it means an error occurred during the copy process
                // and we need to clean up the directory we created.
                if (Directory.Exists(copyPath))
                {
                    Directory.Delete(copyPath, true);
                }
            }
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_copyPath))
        {
            Directory.Delete(_copyPath, true);
        }
    }

    public async Task<BuildOutput> RunDotnetBuild(string csProjName)
    {
        return await RunDotnetCommonBuild("build", csProjName);
    }

    public async Task<BuildOutput> RunDotnetPack(string csProjName)
    {
        return await RunDotnetCommonBuild("pack", csProjName);
    }

    public async Task<BuildOutput> RunDotnetPublish(string csProjName)
    {
        return await RunDotnetCommonBuild("publish", csProjName);
    }

    private async Task<BuildOutput> RunDotnetCommonBuild(string command, string csProjName)
    {
        var args = $"{command} -c Release {csProjName}";
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,

            // Comment this out to see the output in the console window
            //CreateNoWindow = true,
            Arguments = args,
            WorkingDirectory = _copyPath,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var stdOutTask = Task.Run(process.StandardOutput.ReadToEndAsync);
        var processTask = process.WaitForExitAsync();
        var firstToFinish = await Task.WhenAny(processTask, stdOutTask);

        if (!stdOutTask.IsCompleted)
        {
            // The process finished, but the standard output task is still running. It's possible that
            // it is nearly done, so give it some time.
            await Task.WhenAny(stdOutTask, Task.Delay(2000));
        }

        if (!stdOutTask.IsCompleted)
        {
            throw new InvalidOperationException("Did not get output from program");
        }
        var stdOut = await stdOutTask;

        await processTask;
        var outputFolder = Path.Combine(ClonedProjectFolderPath, "bin", "Release");
        return new BuildOutput(process.ExitCode, outputFolder, stdOut);
    }
}
