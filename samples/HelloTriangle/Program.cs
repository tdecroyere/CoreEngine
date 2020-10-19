using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.HostServices;
using CoreEngine.Inputs;
using CoreEngine.Resources;
using CoreEngine.UI.Native;

public static class Program
{
    [UnmanagedCallersOnlyAttribute]
    public static void Main(HostPlatform hostPlatform)
    {
        Logger.WriteMessage("Hello Triangle");
        using var resourcesManager = new ResourcesManager();

        // TODO: Get the config from the host using hardcoded values for the moment
        resourcesManager.AddResourceStorage(new FileSystemResourceStorage("../Resources"));
        resourcesManager.AddResourceStorage(new FileSystemResourceStorage("./Resources"));
            
        var inputsManager = new InputsManager(hostPlatform.InputsService);
        var nativeUIManager = new NativeUIManager(hostPlatform.NativeUIService);

        var window = nativeUIManager.CreateWindow("Hello Triangle", 1280, 720);
        inputsManager.AssociateWindow(window);

        using var graphicsManager = new GraphicsManager(hostPlatform.GraphicsService, resourcesManager);

        var renderTriangleShader = resourcesManager.LoadResourceAsync<Shader>("/RenderTriangle.shader");
        var renderCommandQueue = graphicsManager.CreateCommandQueue(CommandType.Render, "RenderCommandQueue");

        var swapChain = graphicsManager.CreateSwapChain(window, renderCommandQueue, window.Width, window.Height, TextureFormat.Bgra8UnormSrgb);

        var appStatus = new AppStatus() { IsActive = true, IsRunning = true };

        while (appStatus.IsRunning)
        {
            appStatus = nativeUIManager.ProcessSystemMessages();

            if (appStatus.IsActive)
            {
                graphicsManager.ResetCommandQueue(renderCommandQueue);
                var commandList = graphicsManager.CreateCommandList(renderCommandQueue, "RenderTriangle");

                using var swapChainTexture = graphicsManager.GetSwapChainBackBufferTexture(swapChain);
                var renderTargetDescriptor = new RenderTargetDescriptor(swapChainTexture, new System.Numerics.Vector4(0, 0, 1, 1), BlendOperation.None);
                var renderPassDescriptor = new RenderPassDescriptor(renderTargetDescriptor, null, DepthBufferOperation.None, backfaceCulling: false, PrimitiveType.Triangle);
                graphicsManager.BeginRenderPass(commandList, renderPassDescriptor);

                graphicsManager.SetShader(commandList, renderTriangleShader);
                graphicsManager.DrawPrimitives(commandList, PrimitiveType.Triangle, 0, 3);

                graphicsManager.EndRenderPass(commandList);

                graphicsManager.CommitCommandList(commandList);
                graphicsManager.ExecuteCommandLists(renderCommandQueue, new CommandList[] { commandList }, isAwaitable: false);
                
                var presentFence = graphicsManager.PresentSwapChain(swapChain);
                graphicsManager.WaitForCommandQueueOnCpu(presentFence);
                graphicsManager.MoveToNextFrame();
            }
        }
    }
}
