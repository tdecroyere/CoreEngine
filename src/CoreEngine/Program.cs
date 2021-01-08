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

        var context = new CoreEngineContext(systemManagerContainer);
        CoreEngineApp? coreEngineApp = null;
        var pluginManager = new PluginManager();

        if (!string.IsNullOrEmpty(appPath))
        {
            Logger.BeginAction($"Loading CoreEngineApp '{appPath}'");
            coreEngineApp = pluginManager.LoadCoreEngineApp(appPath, context).Result;

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
                var updatedApp = pluginManager.CheckForUpdatedAssemblies().Result;

                if (updatedApp != null)
                {
                    coreEngineApp = updatedApp;
                }
                
                appStatus = nativeUIManager.ProcessSystemMessages();

                if (appStatus.IsActive)
                {
                    systemManagerContainer.PreUpdateSystemManagers();
                    // TOODO: Compute correct delta time
                    coreEngineApp.OnUpdate(context, 1.0f / 60.0f);
                    systemManagerContainer.PostUpdateSystemManagers();

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
