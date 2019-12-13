﻿using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.HostServices;
using CoreEngine.Inputs;
using CoreEngine.Resources;

namespace CoreEngine
{
    // TODO: Make a CoreEngine class
    public static class Bootloader
    {
        private static CoreEngineApp? coreEngineApp = null;
        private static GraphicsManager? graphicsManager = null;
        private static GraphicsSceneQueue? sceneQueue = null;
        private static GraphicsSceneManager? sceneManager = null;

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
                    resourcesManager.AddResourceStorage(new FileSystemResourceStorage("../Resources"));
                    resourcesManager.AddResourceLoader(new SceneResourceLoader(resourcesManager));

                    sceneQueue = new GraphicsSceneQueue();
                    sceneManager = new GraphicsSceneManager(sceneQueue);

                    graphicsManager = new GraphicsManager(hostPlatform.GraphicsService, sceneQueue, resourcesManager);

                    // Register managers
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<ResourcesManager>(resourcesManager);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<GraphicsManager>(graphicsManager);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<GraphicsSceneManager>(sceneManager);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<Graphics2DRenderer>(graphicsManager.Graphics2DRenderer);
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
