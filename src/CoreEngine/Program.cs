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
using CoreEngine.Components;
using CoreEngine.Rendering.Components;

[assembly: InternalsVisibleTo("CoreEngine.UnitTests")]

public static class Program
{
    [UnmanagedCallersOnly(EntryPoint = "main")]
    public static void Main(HostPlatform hostPlatform)
    {
        Logger.BeginAction($"Starting CoreEngine");

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

        var pluginManager = new PluginManager();

        var systemManagerContainer = new SystemManagerContainer();

        // Register managers
        systemManagerContainer.RegisterSystemManager(resourcesManager);
        systemManagerContainer.RegisterSystemManager(sceneManager);
        systemManagerContainer.RegisterSystemManager(nativeUIManager);
        systemManagerContainer.RegisterSystemManager(graphicsManager);
        systemManagerContainer.RegisterSystemManager(renderManager);
        systemManagerContainer.RegisterSystemManager(renderManager.Graphics2DRenderer);
        systemManagerContainer.RegisterSystemManager(inputsManager);
        systemManagerContainer.RegisterSystemManager(pluginManager);

        var context = new CoreEngineContext(systemManagerContainer);
        CoreEngineApp? coreEngineApp = null;

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
                var updatedApp = pluginManager.CheckForUpdatedAssemblies(context).Result;

                if (updatedApp != null)
                {
                    coreEngineApp = updatedApp;
                }
                
                appStatus = nativeUIManager.ProcessSystemMessages();
                context.IsAppActive = appStatus.IsActive;

                systemManagerContainer.PreUpdateSystemManagers(context);
                // TODO: Compute correct delta time
                coreEngineApp.OnUpdate(context, 1.0f / 60.0f);
                systemManagerContainer.PostUpdateSystemManagers(context);
                
                renderManager.Render();
            }
        }

        Logger.WriteMessage("Exiting");
    }
}
