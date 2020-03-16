using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public readonly struct IndirectCommandBuffer : IGraphicsResource
    {
        private readonly GraphicsManager graphicsManager;

        internal IndirectCommandBuffer(GraphicsManager graphicsManager, uint systemId, uint? systemId2, uint? systemId3, int maxCommandCount, GraphicsResourceType resourceType)
        {
            this.graphicsManager = graphicsManager;
            this.GraphicsResourceSystemId = systemId;
            this.GraphicsResourceSystemId2 = systemId2;
            this.GraphicsResourceSystemId3 = systemId3;
            this.MaxCommandCount = maxCommandCount;
            this.ResourceType = resourceType;
        }

        public uint GraphicsResourceId 
        { 
            get
            {
                var result = this.GraphicsResourceSystemId;

                if (ResourceType == GraphicsResourceType.Dynamic && this.GraphicsResourceSystemId2 != null && ((this.graphicsManager.CurrentFrameNumber % 3) == 1))
                {
                    result = this.GraphicsResourceSystemId2.Value;
                }

                else if (ResourceType == GraphicsResourceType.Dynamic && this.GraphicsResourceSystemId3 != null && ((this.graphicsManager.CurrentFrameNumber % 3) == 2))
                {
                    result = this.GraphicsResourceSystemId3.Value;
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

        public uint? GraphicsResourceSystemId3
        {
            get;
        }

        public int MaxCommandCount { get; }
        public GraphicsResourceType ResourceType { get; }
    }
}