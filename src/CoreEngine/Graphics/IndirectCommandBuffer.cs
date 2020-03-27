using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public readonly struct IndirectCommandBuffer : IGraphicsResource
    {
        private readonly GraphicsManager graphicsManager;

        internal IndirectCommandBuffer(GraphicsManager graphicsManager, uint systemId, uint? systemId2, int maxCommandCount, GraphicsResourceType resourceType, string label)
        {
            this.graphicsManager = graphicsManager;
            this.GraphicsResourceSystemId = systemId;
            this.GraphicsResourceSystemId2 = systemId2;
            this.MaxCommandCount = maxCommandCount;
            this.ResourceType = resourceType;
            this.Label = label;
        }

        public uint GraphicsResourceId 
        { 
            get
            {
                var result = this.GraphicsResourceSystemId;

                if (ResourceType == GraphicsResourceType.Dynamic && this.GraphicsResourceSystemId2 != null && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
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

        public int MaxCommandCount { get; }
        public GraphicsResourceType ResourceType { get; }

        public string Label
        {
            get;
        }
    }
}