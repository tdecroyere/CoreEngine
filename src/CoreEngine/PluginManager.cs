using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

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

    // TODO: Unify hot reloading with resource manager?

    public class PluginManager : SystemManager
    {
        private PluginLoadContext? loadContext;
        private IDictionary<string, CoreEngineApp> loadedApplications;
        private IDictionary<string, DateTime> applicationsWriteDates;
        private DateTime lastCheckedDate;
        private IList<Assembly> loadedAssemblies;
        
        public PluginManager()
        {
            this.loadedApplications = new Dictionary<string, CoreEngineApp>();
            this.applicationsWriteDates = new Dictionary<string, DateTime>();
            this.loadedAssemblies = new List<Assembly>();

            this.lastCheckedDate = DateTime.Now;
        }

        public async Task<CoreEngineApp?> LoadCoreEngineApp(string appPath, CoreEngineContext context)
        {
            // TODO: Check if dll exists
            // TODO: Implement target selection

            var currentAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var appBinPath = Path.Combine(currentAssemblyPath, appPath, "bin/win-x64");
            var assemblyPath = Path.Combine(appBinPath, $"{Path.GetFileName(appPath)}.dll");

            if (File.Exists(assemblyPath))
            {
                var resourcesManager = context.SystemManagerContainer.GetSystemManager<ResourcesManager>();
                resourcesManager.AddResourceStorage(new FileSystemResourceStorage(appBinPath));

                var app = await LoadCoreEngineApp(assemblyPath);

                if (app != null)
                {
                    this.loadedApplications.Add(assemblyPath, app);
                    this.applicationsWriteDates.Add(assemblyPath, File.GetLastWriteTime(assemblyPath));
                    app.OnInit(context);
                }

                return app;
            }

            return null;
        }

        // TODO: Refactor that code!
        public async Task<CoreEngineApp?> CheckForUpdatedAssemblies(CoreEngineContext context)
        {
            // TODO: Convert that to a long running task
            var currentDate = DateTime.Now;

            if ((currentDate - this.lastCheckedDate).TotalMilliseconds >= 1000)
            {
                foreach (var assemblyPath in this.loadedApplications.Keys)
                {
                    if (File.Exists(assemblyPath))
                    {
                        var lastWriteTime = File.GetLastWriteTime(assemblyPath);

                        if (lastWriteTime > this.applicationsWriteDates[assemblyPath])
                        {
                            Logger.WriteMessage($"Assembly: {assemblyPath} has changed");

                            var app = await LoadCoreEngineApp(assemblyPath);

                            if (app != null)
                            {
                                this.loadedAssemblies.Clear();

                                if (context.CurrentScene != null)
                                {
                                    context.CurrentScene.EntitySystemManager.UnbindRegisteredSystems();
                                }

                                this.loadedApplications[assemblyPath] = app;
                                this.applicationsWriteDates[assemblyPath] = lastWriteTime;
                                this.lastCheckedDate = currentDate;
                                
                                return app;
                            }
                        }
                    }

                    return this.loadedApplications[assemblyPath];
                }
            }

            return null;
        }

        public Assembly FindLoadedAssembly(string assemblyName)
        {
            for (var i = 0; i < this.loadedAssemblies.Count; i++)
            {
                if (this.loadedAssemblies[i].FullName == assemblyName)
                {
                    return this.loadedAssemblies[i];
                }
            }

            foreach (var loadContext in AssemblyLoadContext.All)
            {
                foreach (var assembly in loadContext.Assemblies)
                {
                    if (assembly.FullName == assemblyName)
                    {
                        return assembly;
                    }
                }
            }

            return Assembly.GetExecutingAssembly();
        }

        private async Task<CoreEngineApp?> LoadCoreEngineApp(string assemblyPath)
        {
            byte[]? assemblyContent = null;
            byte[]? pdbContent = null;
            
            try
            {
                assemblyContent = await File.ReadAllBytesAsync(assemblyPath);

                var pdbPath = Path.Combine(Path.GetDirectoryName(assemblyPath), Path.GetFileNameWithoutExtension(assemblyPath) + ".pdb");

                if (File.Exists(pdbPath))
                {
                    pdbContent = await File.ReadAllBytesAsync(pdbPath);
                }
            }

            catch
            {
                return null;
            }

            if (assemblyContent != null)
            {
                if (this.loadContext == null)
                {
                    this.loadContext = new PluginLoadContext();
                }

                else
                {
                    this.loadContext.Unload();
                    this.loadContext = new PluginLoadContext();

                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                    GC.WaitForPendingFinalizers();
                }

                using var memoryStream = new MemoryStream(assemblyContent);

                MemoryStream? pdbStream = null;

                if (pdbContent != null)
                {
                    pdbStream = new MemoryStream(pdbContent);
                }

                foreach (var test3 in AssemblyLoadContext.All)
                {
                    Logger.BeginAction($"LoadContext: {test3.Name}");

                    foreach (var test2 in test3.Assemblies)
                    {
                        Logger.WriteMessage($"Assembly: {test2.FullName}");
                    }

                    Logger.EndAction();
                }

                var assembly = this.loadContext.LoadFromStream(memoryStream, pdbStream);
                this.loadedAssemblies.Add(assembly);

                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(CoreEngineApp)))
                    {
                        return (CoreEngineApp?)Activator.CreateInstance(type);
                    }
                }
            }
            return null;
        }
    }
}