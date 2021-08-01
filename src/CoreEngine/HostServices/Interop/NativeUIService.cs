using System;
using System.Buffers;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    public unsafe readonly struct NativeUIService : INativeUIService
    {
        private IntPtr context { get; }

        private delegate* unmanaged[Cdecl, SuppressGCTransition]<IntPtr, string, int, int, NativeWindowState, IntPtr> nativeUIService_CreateWindowDelegate { get; }
        public unsafe IntPtr CreateWindow(string title, int width, int height, NativeWindowState windowState)
        {
            if (this.nativeUIService_CreateWindowDelegate != null)
            {
                return this.nativeUIService_CreateWindowDelegate(this.context, title, width, height, windowState);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl, SuppressGCTransition]<IntPtr, IntPtr, string, void> nativeUIService_SetWindowTitleDelegate { get; }
        public unsafe void SetWindowTitle(IntPtr windowPointer, string title)
        {
            if (this.nativeUIService_SetWindowTitleDelegate != null)
            {
                this.nativeUIService_SetWindowTitleDelegate(this.context, windowPointer, title);
            }
        }

        private delegate* unmanaged[Cdecl, SuppressGCTransition]<IntPtr, IntPtr, Vector2> nativeUIService_GetWindowRenderSizeDelegate { get; }
        public unsafe Vector2 GetWindowRenderSize(IntPtr windowPointer)
        {
            if (this.nativeUIService_GetWindowRenderSizeDelegate != null)
            {
                return this.nativeUIService_GetWindowRenderSizeDelegate(this.context, windowPointer);
            }

            return default(Vector2);
        }

        private delegate* unmanaged[Cdecl, SuppressGCTransition]<IntPtr, NativeAppStatus> nativeUIService_ProcessSystemMessagesDelegate { get; }
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
