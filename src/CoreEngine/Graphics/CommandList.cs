using System;

namespace CoreEngine.Graphics
{
    public readonly struct CommandList : IGraphicsResource
    {
        private readonly GraphicsManager graphicsManager;

        internal CommandList(GraphicsManager graphicsManager, uint systemId, uint? systemId2, CommandType type, string label)
        {
            this.graphicsManager = graphicsManager;
            this.GraphicsResourceSystemId = systemId;
            this.GraphicsResourceSystemId2 = systemId2;
            this.Label = label;
            this.ResourceType = GraphicsResourceType.CommandBuffer;
            this.IsStatic = false;
            this.Type = type;
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

        public GraphicsResourceType ResourceType { get; }
        public bool IsStatic { get; }
        public CommandType Type { get; }
    }
}