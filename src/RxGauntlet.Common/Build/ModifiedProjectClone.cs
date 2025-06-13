using System.Diagnostics;
using System.Xml;

namespace RxGauntlet.Build;

public sealed class ModifiedProjectClone : IDisposable
{
    private readonly string copyPath;

    private ModifiedProjectClone(string copyPath)
    {
        this.copyPath = copyPath;
    }

    public string ClonedProjectFolderPath => copyPath;

    public static ModifiedProjectClone Create(
        string sourceProjectFolder,
        string copyParentFolderName,
        Action<string, XmlDocument> modifyProjectFile)
    {
        string copyPath = Path.Combine(
            Path.GetTempPath(),
            "RxGauntlet",
            copyParentFolderName,
            DateTime.Now.ToString("yyyyMMdd-HHmmss"));

        Directory.CreateDirectory(copyPath);

        ModifiedProjectClone? clone = new ModifiedProjectClone(copyPath);
        try
        {
            foreach (string file in Directory.GetFiles(sourceProjectFolder))
            {
                string extension = Path.GetExtension(file).ToLowerInvariant();
                string relativePath = Path.GetRelativePath(sourceProjectFolder, file);
                string destinationPath = Path.Combine(copyPath, relativePath);

                switch (extension)
                {
                    case ".cs":
                        File.Copy(file, destinationPath, true);
                        break;

                    case ".csproj":
                        XmlDocument csProjXmlDoc = new();
                        csProjXmlDoc.Load(file);
                        modifyProjectFile(file, csProjXmlDoc);
                        csProjXmlDoc.Save(destinationPath);
                        break;
                }
            }

            // We're now going to return without error, so we no longer want the finally block
            // to delete the directory. That will now happen when the caller calls Dispose on
            // the ModiyfiedProjectClone we return..
            ModifiedProjectClone result = clone;
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
        if (Directory.Exists(copyPath))
        {
            Directory.Delete(copyPath, true);
        }
    }

    public async Task<int> RunDotnetBuild(string csProjName)
    {
        return await RunDotnet($"build -c Release {csProjName}");
    }

    public async Task<int> RunDotnetPublish(string csProjName)
    {
        return await RunDotnet($"publish -c Release {csProjName}");
        //var startInfo = new ProcessStartInfo
        //{
        //    FileName = "dotnet",
        //    UseShellExecute = false,

        //    // Comment this out to see the output in the console window
        //    //CreateNoWindow = true,
        //    Arguments = $"publish -c Release {csProjName}",
        //    WorkingDirectory = copyPath,
        //};

        //using var process = new Process { StartInfo = startInfo };
        //process.Start(); 
        //await process.WaitForExitAsync();

        //Console.WriteLine($"dotnet publish exit code: {process.ExitCode}");
        //return process.ExitCode;
    }

    private async Task<int> RunDotnet(string args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,

            // Comment this out to see the output in the console window
            //CreateNoWindow = true,
            Arguments = args,
            WorkingDirectory = copyPath,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync();

        Console.WriteLine($"dotnet exit code: {process.ExitCode}");
        return process.ExitCode;
    }
}
