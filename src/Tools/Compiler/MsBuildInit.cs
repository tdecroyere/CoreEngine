using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.Build.Locator;

namespace CoreEngine.Tools.Compiler
{
    public static class MSBuildInit
    {
        [ModuleInitializer]
        public static void Init()
        {
            try
            {				
                if (!MSBuildLocator.IsRegistered)
                {
                    var instance = MSBuildLocator.RegisterDefaults();
                    
                    AssemblyLoadContext.Default.Resolving += (assemblyLoadContext, assemblyName) =>
                    {
                        var path = Path.Combine(instance.MSBuildPath, assemblyName.Name + ".dll");
                        
                        if (File.Exists(path))
                        {
                            return assemblyLoadContext.LoadFromAssemblyPath(path);
                        }

                        return null;
                    };
                }
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }
    }
}