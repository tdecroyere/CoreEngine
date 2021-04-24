using System.Numerics;
using CoreEngine.HostServices;

namespace CoreEngine.UI.Native
{
    public class NativeUIManager : SystemManager
    {
        private readonly INativeUIService nativeUIService;

        public NativeUIManager(INativeUIService nativeUIService)
        {
            this.nativeUIService = nativeUIService;
        }

        public Window CreateWindow(string title, int width, int height, WindowState windowState = WindowState.Normal)
        {
            var nativePointer = this.nativeUIService.CreateWindow(title, width, height, (NativeWindowState)windowState);
            // TODO: GetClientRect
            return new Window(nativePointer, title, width, height);
        }

        public void SetWindowTitle(Window window, string title)
        {
            this.nativeUIService.SetWindowTitle(window.NativePointer, title);
        }

        public Vector2 GetWindowRenderSize(Window window)
        {
            return this.nativeUIService.GetWindowRenderSize(window.NativePointer);
        }

        public AppStatus ProcessSystemMessages()
        {
            return new AppStatus(this.nativeUIService.ProcessSystemMessages());
        }
    }
}