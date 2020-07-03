using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.HostServices;
using CoreEngine.Inputs;
using CoreEngine.Resources;
using CoreEngine.Rendering;

[assembly: InternalsVisibleTo("CoreEngine.UnitTests")]

namespace CoreEngine
{
    public delegate void StartEngineDelegate(string appName, ref HostPlatform hostPlatform);
    public delegate void UpdateEngineDelegate(float deltaTime);
    
    // TODO: Make a CoreEngine class
    public static class Bootloader
    {
        private static CoreEngineApp? coreEngineApp = null;
        private static GraphicsManager? graphicsManager = null;
        private static RenderManager? renderManager = null;
        private static GraphicsSceneQueue? sceneQueue = null;
        private static GraphicsSceneManager? sceneManager = null;

        public static void StartEngine(string appName, ref HostPlatform hostPlatform)
        {
            Logger.BeginAction("Starting CoreEngine");

            if (appName != null)
            {
                var resourcesManager = new ResourcesManager();
                    
                // TODO: Get the config from the host using hardcoded values for the moment
                resourcesManager.AddResourceStorage(new FileSystemResourceStorage("../Resources"));
                resourcesManager.AddResourceStorage(new FileSystemResourceStorage("./Resources"));
                resourcesManager.AddResourceLoader(new SceneResourceLoader(resourcesManager));

                sceneQueue = new GraphicsSceneQueue();
                sceneManager = new GraphicsSceneManager(sceneQueue);

                graphicsManager = new GraphicsManager(hostPlatform.GraphicsService, resourcesManager);
                renderManager = new RenderManager(graphicsManager, resourcesManager, sceneQueue);

                var systemManagerContainer = new SystemManagerContainer();

                // Register managers
                systemManagerContainer.RegisterSystemManager<ResourcesManager>(resourcesManager);
                systemManagerContainer.RegisterSystemManager<GraphicsSceneManager>(sceneManager);
                systemManagerContainer.RegisterSystemManager<GraphicsManager>(graphicsManager);
                systemManagerContainer.RegisterSystemManager<RenderManager>(renderManager);
                systemManagerContainer.RegisterSystemManager<Graphics2DRenderer>(renderManager.Graphics2DRenderer);
                systemManagerContainer.RegisterSystemManager<InputsManager>(new InputsManager(hostPlatform.InputsService));

                Logger.BeginAction($"Loading CoreEngineApp '{appName}'");
                coreEngineApp = LoadCoreEngineApp(appName, systemManagerContainer).Result;
                resourcesManager.WaitForPendingResources();

                if (coreEngineApp != null)
                {
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

            if (renderManager != null)
            {
                renderManager.Render();
            }
        }

        // TODO: Use the isolated app domain new feature to be able to do hot build of the app dll
        private static async Task<CoreEngineApp?> LoadCoreEngineApp(string appName, SystemManagerContainer systemManagerContainer)
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
                        return (CoreEngineApp?)Activator.CreateInstance(type, systemManagerContainer);
                    }
                }
            }

            return null;
        }
    }
}
