// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Autodesk.Revit.UI;
using System.Reflection;

namespace SAM.Revit.UI.Classes
{
    public sealed class ExternalApplication : IExternalApplication
    {
        private static readonly AssemblyResolver assemblyResolver = new();

        private readonly List<IExternalApplication> externalApplications = [];

        public Result OnShutdown(UIControlledApplication application)
        {
            foreach (IExternalApplication externalApplication in externalApplications)
            {
                externalApplication?.OnShutdown(application);
            }

            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            string path = Assembly.GetExecutingAssembly().Location;
            string directory_Revit = Path.GetDirectoryName(path)!;

            string directory_SAM = Path.GetDirectoryName(directory_Revit)!;

            assemblyResolver.Enable(
              managedDirectories:
              [
                directory_Revit,
                // Revit ships a version-specific native dependency set. Resolve its
                // managed entry points from the same directory before the shared SAM
                // folder so an interop assembly cannot bind to a different native ABI.
                directory_SAM,
                //Path.Combine(directory, "lib")
              ],
              nativeDirectories:
              [
                directory_Revit,
                Path.Combine(directory_Revit, "runtimes", "win-x64", "native"),
                directory_SAM,
                Path.Combine(directory_SAM, "runtimes", "win-x64", "native"),
              ]
            );

            // Optional: pin a specific version if you ship it (example)
            //assemblyResolver.AddRedirect("Newtonsoft.Json","Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed");

            List<string> names = ["SAM.Core.Revit.UI.dll"];

            foreach (string name in names)
            {
                string path_Temp = Path.Combine(directory_Revit, name);

                IExternalApplication? externalApplication = LoadExternalApplication(path_Temp);
                if (externalApplication is null)
                {
                    continue;
                }

                externalApplication.OnStartup(application);
            }

            //assemblyResolver.Disable();

            return Result.Succeeded;
        }

        private static IExternalApplication? LoadExternalApplication(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                Assembly? assembly = Assembly.LoadFrom(path);
                if (assembly is not null)
                {
                    Type? type = assembly.GetTypes().FirstOrDefault(t => typeof(IExternalApplication).IsAssignableFrom(t) && !t.IsAbstract);
                    if (type is not null)
                    {
                        if (Activator.CreateInstance(type) is IExternalApplication result)
                        {
                            return result;
                        }
                    }
                }
            }
            catch
            {

            }

            return null;
        }
    }
}
