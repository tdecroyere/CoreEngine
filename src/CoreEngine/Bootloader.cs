using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.HostServices;
using CoreEngine.Inputs;
using CoreEngine.Resources;

namespace CoreEngine
{
    public static class Bootloader
    {
        private static CoreEngineApp? coreEngineApp = null;
        private static GraphicsSceneRenderer? sceneRenderer = null;

        public static void StartEngine(string appName, ref HostPlatform hostPlatform)
        {
            Logger.WriteMessage("Starting CoreEngine...");

            if (appName != null)
            {
                Logger.WriteMessage($"Loading CoreEngineApp '{appName}'...", LogMessageTypes.Action);
                coreEngineApp = LoadCoreEngineApp(appName).Result;

                if (coreEngineApp != null)
                {
                    Logger.WriteMessage("CoreEngineApp loading successfull.", LogMessageTypes.Success);

                    var resourcesManager = new ResourcesManager();
                    
                    // TODO: Get the config from the host using hardcoded values for the moment
                    resourcesManager.AddResourceStorage(new FileSystemResourceStorage("."));
                    resourcesManager.AddResourceLoader(new SceneResourceLoader(resourcesManager));

                    var graphicsManager = new GraphicsManager(hostPlatform.GraphicsService, resourcesManager);

                    // Register managers
                    sceneRenderer = new GraphicsSceneRenderer(hostPlatform.GraphicsService, graphicsManager);

                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<ResourcesManager>(resourcesManager);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<GraphicsManager>(graphicsManager);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<GraphicsSceneRenderer>(sceneRenderer);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<InputsManager>(new InputsManager(hostPlatform.InputsService));

                    Logger.WriteMessage("Initializing app...", LogMessageTypes.Action);
                    coreEngineApp.Init();
                    Logger.WriteMessage("Initializing app done.", LogMessageTypes.Success);
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
            if (sceneRenderer != null)
            {
                sceneRenderer.Render();
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
