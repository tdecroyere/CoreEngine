using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsBuffer : IGraphicsResource
    {
        private readonly GraphicsManager graphicsManager;

        internal GraphicsBuffer(GraphicsManager graphicsManager, uint systemId, uint? systemId2, int length, bool isStatic, string label)
        {
            this.graphicsManager = graphicsManager;
            this.GraphicsResourceSystemId = systemId;
            this.GraphicsResourceSystemId2 = systemId2;
            this.Length = length;
            this.IsStatic = isStatic;
            this.ResourceType = GraphicsResourceType.Buffer;
            this.Label = label;
        }

        public uint GraphicsResourceId 
        { 
            get
            {
                var result = this.GraphicsResourceSystemId;

                if (!IsStatic && this.GraphicsResourceSystemId2 != null && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
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

        public int Length { get; }

        public GraphicsResourceType ResourceType { get; }
        public bool IsStatic { get; }

        public string Label
        {
            get;
        }
    }
}