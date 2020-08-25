using System;
using System.Buffers;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    public unsafe struct NativeUIService : INativeUIService
    {
        private IntPtr context { get; }

        private delegate* cdecl<IntPtr, string, int, int, IntPtr> nativeUIService_CreateWindowDelegate { get; }
        public unsafe IntPtr CreateWindow(string title, int width, int height)
        {
            if (this.context != null && this.nativeUIService_CreateWindowDelegate != null)
            {
                return this.nativeUIService_CreateWindowDelegate(this.context, title, width, height);
            }

            return default(IntPtr);
        }

        private delegate* cdecl<IntPtr, IntPtr, Vector2> nativeUIService_GetWindowRenderSizeDelegate { get; }
        public unsafe Vector2 GetWindowRenderSize(IntPtr windowPointer)
        {
            if (this.context != null && this.nativeUIService_GetWindowRenderSizeDelegate != null)
            {
                return this.nativeUIService_GetWindowRenderSizeDelegate(this.context, windowPointer);
            }

            return default(Vector2);
        }
    }
}
