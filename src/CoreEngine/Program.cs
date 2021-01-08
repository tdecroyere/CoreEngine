using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CoreEngine;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.HostServices;
using CoreEngine.Inputs;
using CoreEngine.Rendering;
using CoreEngine.Resources;
using CoreEngine.UI.Native;

[assembly: InternalsVisibleTo("CoreEngine.UnitTests")]

public static class Program
{
    [UnmanagedCallersOnly(EntryPoint = "main")]
    public static void Main(HostPlatform hostPlatform)
    {
        Logger.BeginAction($"Starting CoreEngine (EcsTest)");

        var args = Utils.GetCommandLineArguments();
        var appPath = string.Empty;

        if (args.Length > 0)
        {
            appPath = args[0];
        }

        var resourcesManager = new ResourcesManager();
            
        // TODO: Get the config from the host using hardcoded values for the moment
        resourcesManager.AddResourceStorage(new FileSystemResourceStorage("../Resources"));
        resourcesManager.AddResourceStorage(new FileSystemResourceStorage("./Resources"));
        resourcesManager.AddResourceLoader(new SceneResourceLoader(resourcesManager));

        var inputsManager = new InputsManager(hostPlatform.InputsService);
        var nativeUIManager = new NativeUIManager(hostPlatform.NativeUIService);

        var window = nativeUIManager.CreateWindow("Core Engine", 1280, 720);
        inputsManager.AssociateWindow(window);

        var sceneQueue = new GraphicsSceneQueue();
        var sceneManager = new GraphicsSceneManager(sceneQueue);

        using var graphicsManager = new GraphicsManager(hostPlatform.GraphicsService, resourcesManager);
        using var renderManager = new RenderManager(window, nativeUIManager, graphicsManager, resourcesManager, sceneQueue);

        var systemManagerContainer = new SystemManagerContainer();

        // Register managers
        systemManagerContainer.RegisterSystemManager<ResourcesManager>(resourcesManager);
        systemManagerContainer.RegisterSystemManager<GraphicsSceneManager>(sceneManager);
        systemManagerContainer.RegisterSystemManager<NativeUIManager>(nativeUIManager);
        systemManagerContainer.RegisterSystemManager<GraphicsManager>(graphicsManager);
        systemManagerContainer.RegisterSystemManager<RenderManager>(renderManager);
        systemManagerContainer.RegisterSystemManager<Graphics2DRenderer>(renderManager.Graphics2DRenderer);
        systemManagerContainer.RegisterSystemManager<InputsManager>(inputsManager);

        CoreEngineApp? coreEngineApp = null;

        if (!string.IsNullOrEmpty(appPath))
        {
            Logger.BeginAction($"Loading CoreEngineApp '{appPath}'");
            var pluginManager = new PluginManager();
            coreEngineApp = pluginManager.LoadCoreEngineApp(appPath, systemManagerContainer).Result;

            if (coreEngineApp != null)
            {
                resourcesManager.WaitForPendingResources();
                nativeUIManager.SetWindowTitle(window, coreEngineApp.Name);
             
                Logger.EndAction();
            }

            else
            {
                Logger.EndActionError();
            }

            Logger.EndAction();
        }

        if (coreEngineApp != null)
        {
            var appStatus = new AppStatus() { IsActive = true, IsRunning = true };

            while (appStatus.IsRunning)
            {
                appStatus = nativeUIManager.ProcessSystemMessages();

                if (appStatus.IsActive)
                {
                    coreEngineApp.SystemManagerContainer.PreUpdateSystemManagers();
                    coreEngineApp.Update(1.0f / 60.0f);
                    coreEngineApp.SystemManagerContainer.PostUpdateSystemManagers();

                    if (renderManager != null)
                    {
                        renderManager.Render();
                    }
                }
            }
        }

        Logger.WriteMessage("Exiting");
    }
}
