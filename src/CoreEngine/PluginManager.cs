using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;

namespace CoreEngine
{
    public class PluginLoadContext : AssemblyLoadContext
    {
        public PluginLoadContext() : base(isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.FullName == Assembly.GetExecutingAssembly().FullName)
            {
                return Assembly.GetExecutingAssembly();
            }

            return null;
        }
    }

    public class PluginManager
    {
        private PluginLoadContext loadContext;
        
        public PluginManager()
        {
            this.loadContext = new PluginLoadContext();
        }

        public async Task<CoreEngineApp?> LoadCoreEngineApp(string appPath, SystemManagerContainer systemManagerContainer)
        {
            // TODO: Check if dll exists
            var currentAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assemblyPath = Path.Combine(currentAssemblyPath, appPath, "bin/win-x64", $"{Path.GetFileName(appPath)}.dll");

            if (File.Exists(assemblyPath))
            {
                var assemblyContent = await File.ReadAllBytesAsync(assemblyPath);
                using var memoryStream = new MemoryStream(assemblyContent);

                foreach (var test3 in AssemblyLoadContext.All)
                {
                    Logger.BeginAction($"LoadContext: {test3.Name}");

                    foreach (var test2 in test3.Assemblies)
                    {
                        Logger.WriteMessage($"Assembly: {test2.FullName}");
                    }

                    Logger.EndAction();
                }

                var assembly = this.loadContext.LoadFromStream(memoryStream);

                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(CoreEngineApp)))
                    {
                        return (CoreEngineApp?)Activator.CreateInstance(type, systemManagerContainer);
                    }
                }
            }

            return null;
        }
    }
}