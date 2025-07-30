// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

namespace RxGauntlet.Build;

public record BuildOutput(
    int BuildProcessExitCode,
    string OutputFolder,
    string BuildStdOut)
{
    public bool BuildSucceeded => BuildProcessExitCode == 0;
}

public record BuildAndRunOutput(
    int BuildProcessExitCode,
    string OutputFolder,
    string BuildStdOut,
    int? ExecuteExitCode,
    string? ExecuteStdOut,
    string? ExecuteStdErr) : BuildOutput(BuildProcessExitCode, OutputFolder, BuildStdOut);
