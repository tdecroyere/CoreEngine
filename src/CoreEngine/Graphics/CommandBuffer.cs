using System;

namespace CoreEngine.Graphics
{
    public readonly struct CommandBuffer : IGraphicsResource
    {
        private readonly GraphicsManager graphicsManager;

        internal CommandBuffer(GraphicsManager graphicsManager, uint systemId, uint? systemId2, string label)
        {
            this.graphicsManager = graphicsManager;
            this.GraphicsResourceSystemId = systemId;
            this.GraphicsResourceSystemId2 = systemId2;
            this.Label = label;
        }

        public uint GraphicsResourceId 
        { 
            get
            {
                var result = this.GraphicsResourceSystemId;

                if (this.GraphicsResourceSystemId2 != null && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
                {
                    result = this.GraphicsResourceSystemId2.Value;
                }

                return result;
            }
        }

        public uint GraphicsResourceSystemId
        {
            get;
        }

        public uint? GraphicsResourceSystemId2
        {
            get;
        }

        public string Label
        {
            get;
        }
    }
}