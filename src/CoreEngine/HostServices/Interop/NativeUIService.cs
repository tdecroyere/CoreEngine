using System;
using System.Buffers;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    public unsafe struct NativeUIService : INativeUIService
    {
        private IntPtr context { get; }

        private delegate* unmanaged[Cdecl]<IntPtr, string, int, int, NativeWindowState, IntPtr> nativeUIService_CreateWindowDelegate { get; }
        public unsafe IntPtr CreateWindow(string title, int width, int height, NativeWindowState windowState)
        {
            if (this.nativeUIService_CreateWindowDelegate != null)
            {
                return this.nativeUIService_CreateWindowDelegate(this.context, title, width, height, windowState);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, Vector2> nativeUIService_GetWindowRenderSizeDelegate { get; }
        public unsafe Vector2 GetWindowRenderSize(IntPtr windowPointer)
        {
            if (this.nativeUIService_GetWindowRenderSizeDelegate != null)
            {
                return this.nativeUIService_GetWindowRenderSizeDelegate(this.context, windowPointer);
            }

            return default(Vector2);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, NativeAppStatus> nativeUIService_ProcessSystemMessagesDelegate { get; }
        public unsafe NativeAppStatus ProcessSystemMessages()
        {
            if (this.nativeUIService_ProcessSystemMessagesDelegate != null)
            {
                return this.nativeUIService_ProcessSystemMessagesDelegate(this.context);
            }

            return default(NativeAppStatus);
        }
    }
}
