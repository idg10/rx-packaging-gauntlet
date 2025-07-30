// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

using PlugIn.HostDriver;
using PlugIn.HostSerialization;

namespace CheckIssue97;

public record PlugInTestResult(
    string HostTargetFrameworkMoniker,
    PlugInDescriptor PlugIn1,
    PlugInDescriptor PlugIn2,
    HostOutput Result);
