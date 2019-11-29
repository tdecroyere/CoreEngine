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
        private static GraphicsManager? graphicsManager = null;
        private static GraphicsDebugRenderer? debugRenderer = null;
        private static GraphicsSceneRenderer? sceneRenderer = null;

        public static void StartEngine(string appName, ref HostPlatform hostPlatform)
        {
            Logger.BeginAction("Starting CoreEngine");

            if (appName != null)
            {
                Logger.BeginAction($"Loading CoreEngineApp '{appName}'");
                coreEngineApp = LoadCoreEngineApp(appName).Result;

                if (coreEngineApp != null)
                {
                    var resourcesManager = new ResourcesManager();
                    
                    // TODO: Get the config from the host using hardcoded values for the moment
                    resourcesManager.AddResourceStorage(new FileSystemResourceStorage("."));
                    resourcesManager.AddResourceLoader(new SceneResourceLoader(resourcesManager));

                    graphicsManager = new GraphicsManager(hostPlatform.GraphicsService, resourcesManager);
                    debugRenderer = new GraphicsDebugRenderer(graphicsManager);

                    // Register managers
                    sceneRenderer = new GraphicsSceneRenderer(graphicsManager, debugRenderer);

                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<ResourcesManager>(resourcesManager);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<GraphicsManager>(graphicsManager);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<GraphicsDebugRenderer>(debugRenderer);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<GraphicsSceneRenderer>(sceneRenderer);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<InputsManager>(new InputsManager(hostPlatform.InputsService));

                    coreEngineApp.Init();
                    Logger.EndAction();
                }

                else
                {
                    Logger.EndActionError();
                }
            }

            Logger.EndAction();
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

            if (debugRenderer != null)
            {
                debugRenderer.Render();
            }
        }

        // TODO: Use the isolated app domain new feature to be able to do hot build of the app dll
        private static async Task<CoreEngineApp?> LoadCoreEngineApp(string appName)
        {
            // TODO: Check if dll exists
            var currentAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assemblyPath = Path.Combine(currentAssemblyPath, $"{appName}.dll");

            if (File.Exists(assemblyPath))
            {
                var assemblyContent = await File.ReadAllBytesAsync(assemblyPath);
                var assembly = Assembly.Load(assemblyContent);

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
