using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Inputs;
using CoreEngine.Resources;

namespace CoreEngine
{
    public static class Bootloader
    {
        private static CoreEngineApp? coreEngineApp = null;
        private static SceneRenderer? graphicsManager = null;

        public static void StartEngine(string appName, ref HostPlatform hostPlatform)
        {
            Logger.WriteMessage("Starting CoreEngine...");

            if (appName != null)
            {
                Logger.WriteMessage($"Loading CoreEngineApp '{appName}'...");
                coreEngineApp = LoadCoreEngineApp(appName).Result;

                if (coreEngineApp != null)
                {
                    Logger.WriteMessage("CoreEngineApp loading successfull.");

                    var resourcesManager = new ResourcesManager();
                    
                    // TODO: Get the config from the host using hardcoded values for the moment
                    resourcesManager.AddResourceStorage(new FileSystemResourceStorage("."));
                    resourcesManager.AddResourceLoader(new SceneResourceLoader(resourcesManager));

                    // Register managers
                    graphicsManager = new SceneRenderer(hostPlatform.GraphicsService, hostPlatform.MemoryService, resourcesManager);

                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<ResourcesManager>(resourcesManager);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<SceneRenderer>(graphicsManager);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<InputsManager>(new InputsManager(hostPlatform.InputsService));

                    Logger.WriteMessage("Initializing app...");
                    coreEngineApp.Init();
                    Logger.WriteMessage("Initializing app done.");
                }
            }
        }

        public static void UpdateEngine(float deltaTime)
        {
            if (coreEngineApp != null)
            {
                coreEngineApp.SystemManagerContainer.PreUpdateSystemManagers();
                coreEngineApp.Update(deltaTime);
                coreEngineApp.SystemManagerContainer.PostUpdateSystemManagers();
            }
        }

        public static void Render()
        {
            if (graphicsManager != null)
            {
                graphicsManager.Render();
            }
        }

        // TODO: Use the isolated app domain new feature to be able to do hot build of the app dll
        private static async Task<CoreEngineApp?> LoadCoreEngineApp(string appName)
        {
            // TODO: Check if dll exists
            var currentAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assemblyContent = await File.ReadAllBytesAsync(Path.Combine(currentAssemblyPath, $"{appName}.dll"));
            var assembly = Assembly.Load(assemblyContent);

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(CoreEngineApp)))
                {
                    return (CoreEngineApp?)Activator.CreateInstance(type);
                }
            }

            return null;
        }
    }
}
