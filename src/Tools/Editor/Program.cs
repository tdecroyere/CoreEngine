using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.HostServices;
using CoreEngine.Resources;
using CoreEngine.UI.Native;

public static class Program
{
    [UnmanagedCallersOnly(EntryPoint = "main")]
    public static void Main(HostPlatform hostPlatform)
    {
        Logger.BeginAction("Starting CoreEngine Editor");

        using var resourcesManager = new ResourcesManager();
        resourcesManager.AddResourceStorage(new FileSystemResourceStorage("./Resources"));

        var nativeUIManager = new NativeUIManager(hostPlatform.NativeUIService);
        var window = nativeUIManager.CreateWindow("Core Engine Editor", 1280, 720, WindowState.Maximized);

        using var graphicsManager = new GraphicsManager(hostPlatform.GraphicsService, resourcesManager);

        Logger.EndAction();

        var appStatus = new AppStatus() { IsActive = true, IsRunning = true };

        while (appStatus.IsRunning)
        {
            Logger.WriteMessage("OK");
            appStatus = nativeUIManager.ProcessSystemMessages();

            if (appStatus.IsActive)
            {
                
            }
        }
    }
}
