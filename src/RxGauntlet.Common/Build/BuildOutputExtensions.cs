// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

namespace RxGauntlet.Build;

public static class BuildOutputExtensions
{
    public static UiFrameworkComponentsInOutput CheckForUiComponentsInOutput(this BuildOutput buildResult)
    {
        var includesWpf = false;
        var includesWindowsForms = false;
        foreach (var file in Directory.GetFiles(buildResult.OutputFolder, "*", new EnumerationOptions { RecurseSubdirectories = true }))
        {
            var filename = Path.GetFileName(file);
            if (filename.Equals("PresentationFramework.dll", StringComparison.InvariantCultureIgnoreCase))
            {
                includesWpf = true;
            }

            if (filename.Equals("System.Windows.Forms.dll", StringComparison.InvariantCultureIgnoreCase))
            {
                includesWindowsForms = true;
            }
        }

        return new UiFrameworkComponentsInOutput(
            WpfPresent: includesWpf,
            WindowsFormsPresent: includesWindowsForms);
    }
}
