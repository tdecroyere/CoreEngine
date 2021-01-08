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

    public class PluginManager
    {
        private PluginLoadContext? loadContext;
        private IDictionary<string, CoreEngineApp> loadedApplications;
        private IDictionary<string, DateTime> applicationsWriteDates;
        private DateTime lastCheckedDate;
        
        public PluginManager()
        {
            this.loadedApplications = new Dictionary<string, CoreEngineApp>();
            this.applicationsWriteDates = new Dictionary<string, DateTime>();

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
        public async Task<CoreEngineApp?> CheckForUpdatedAssemblies()
        {
            // TODO: Convert that to a long running task
            var currentDate = DateTime.Now;

            if ((currentDate - this.lastCheckedDate).TotalMilliseconds >= 1000)
            {
                this.lastCheckedDate = currentDate;

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
                                this.loadedApplications[assemblyPath] = app;
                                this.applicationsWriteDates[assemblyPath] = lastWriteTime;
                                return app;
                            }
                        }
                    }

                    return this.loadedApplications[assemblyPath];
                }
            }

            return null;
        }

        private async Task<CoreEngineApp?> LoadCoreEngineApp(string assemblyPath)
        {
            byte[]? assemblyContent = null;
            
            try
            {
                assemblyContent = await File.ReadAllBytesAsync(assemblyPath);
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

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

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
                        return (CoreEngineApp?)Activator.CreateInstance(type);
                    }
                }
            }
            return null;
        }
    }
}