using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

public static class Program
{
    [UnmanagedCallersOnlyAttribute]
    public static void Main(HostPlatform hostPlatform)
    {
        Logger.BeginAction("Starting CoreEngine Compiler");

        var resourcesManager = new ResourcesManager();
        resourcesManager.AddResourceStorage(new FileSystemResourceStorage("./Resources"));

        var graphicsManager = new GraphicsManager(hostPlatform.GraphicsService, resourcesManager);


        Logger.EndAction();
    }
}
