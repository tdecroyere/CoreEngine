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
using CoreEngine.UI.Native;
using CoreEngine;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("CoreEngine.UnitTests")]

public static class Program
{
    [UnmanagedCallersOnlyAttribute]
    public static void Main(HostPlatform hostPlatform)
    {
        Logger.BeginAction("Starting CoreEngine");

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

        Logger.BeginAction($"Loading CoreEngineApp 'EcsTest'");
        var coreEngineApp = LoadCoreEngineApp("EcsTest", systemManagerContainer).Result;
        resourcesManager.WaitForPendingResources();

        if (coreEngineApp != null)
        {
            Logger.EndAction();
        }

        else
        {
            Logger.EndActionError();
        }

        Logger.EndAction();

        var appStatus = new AppStatus() { IsActive = true, IsRunning = true };

        while (appStatus.IsRunning)
        {
            appStatus = nativeUIManager.ProcessSystemMessages();

            if (appStatus.IsActive)
            {
                if (coreEngineApp != null)
                {
                    coreEngineApp.SystemManagerContainer.PreUpdateSystemManagers();
                    coreEngineApp.Update(1.0f / 60.0f);
                    coreEngineApp.SystemManagerContainer.PostUpdateSystemManagers();
                }

                if (renderManager != null)
                {
                    renderManager.Render();
                }
            }
        }

        Logger.WriteMessage("Exiting");

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
