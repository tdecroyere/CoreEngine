using System;
using System.Collections.Generic;

namespace CoreEngine.Graphics
{
    public readonly struct CommandQueue : IDisposable
    {
        private readonly GraphicsManager graphicsManager;

        internal CommandQueue(GraphicsManager graphicsManager, IntPtr nativePointer, CommandType type, string label)
        {
            this.graphicsManager = graphicsManager;
            this.NativePointer = nativePointer;
            this.Type = type;
            this.Label = label;
            this.commandListFreeList = new Stack<CommandList>();
            this.CurrentCopyBuffers = new List<GraphicsBuffer>();
        }

        public void Dispose()
        {
            var commandList = this.graphicsManager.CreateCommandList(this, "DisposeCommandList");
            this.graphicsManager.CommitCommandList(commandList);
            var fence = this.graphicsManager.ExecuteCommandLists(this, new CommandList[] { commandList });
            this.graphicsManager.WaitForCommandQueueOnCpu(fence);

            while (this.commandListFreeList.Count > 0)
            {
                var commandListToDelete = this.commandListFreeList.Pop();
                this.graphicsManager.DeleteCommandList(commandListToDelete);
            }

            this.graphicsManager.DeleteCommandQueue(this);

            GC.SuppressFinalize(this);
        }

        public readonly IntPtr NativePointer { get; }
        public readonly CommandType Type { get; }
        public readonly string Label { get; }
        public readonly Stack<CommandList> commandListFreeList { get; }
        public readonly IList<GraphicsBuffer> CurrentCopyBuffers { get; }
    }
}