// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// GenerateAssemblyInfo is false (this file supplies attributes by hand), so
// the SDK never emits its usual implicit
// [assembly: SupportedOSPlatform("windows...")] for this -windows project
// (net8.0-windows for Revit 2025/2026, net10.0-windows for Revit 2027).
// Without it, the CA1416 platform-compatibility analyzer cannot tell this
// assembly is Windows-only and flags every WinForms/WPF/Revit-UI API call
// site as "reachable on all platforms" - the actual runtime constraint has
// not changed, only the analyzer's visibility into it.
[assembly: SupportedOSPlatform("windows")]

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SAM.Analytical.Revit.Addin")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SAM.Analytical.Revit.Addin")]
[assembly: AssemblyCopyright("Copyright ©  2022")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d835aef0-8c2c-4345-8f76-062669bcd3e2")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
