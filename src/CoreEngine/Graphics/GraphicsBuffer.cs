using System;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsBuffer : IGraphicsResource
    {
        private readonly GraphicsManager graphicsManager;

        internal GraphicsBuffer(GraphicsManager graphicsManager, uint systemId, uint? systemId2, int sizeInBytes, GraphicsResourceType resourceType)
        {
            this.graphicsManager = graphicsManager;
            this.SystemId = systemId;
            this.SystemId2 = systemId2;
            this.SizeInBytes = sizeInBytes;
            this.ResourceType = resourceType;
        }

        public readonly uint Id 
        { 
            get
            {
                var result = this.SystemId;

                if (ResourceType == GraphicsResourceType.Dynamic && this.SystemId2 != null && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
                {
                    result = this.SystemId2.Value;
                }

                return result;
            }
        }

        public readonly uint SystemId
        {
            get;
        }

        public readonly uint? SystemId2
        {
            get;
        }

        public readonly int SizeInBytes { get; }
        public readonly GraphicsResourceType ResourceType { get; }
    }
}