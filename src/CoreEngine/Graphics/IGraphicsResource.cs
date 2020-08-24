using System;

namespace CoreEngine.Graphics
{
    public interface IGraphicsResource
    {
        IntPtr NativePointer { get; }
        IntPtr NativePointer1 { get; }
        IntPtr? NativePointer2 { get; }
        bool IsStatic { get; }
        GraphicsResourceType ResourceType { get; }
        string Label { get; }

        // TODO: To Activate
        // GraphicsMemoryAllocation GraphicsMemoryAllocation { get; }
    }
}