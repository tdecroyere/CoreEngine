using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Texture : Resource, IGraphicsResource
    {
        private readonly GraphicsManager graphicsManager;

        internal Texture(GraphicsManager graphicsManager, uint systemId, uint? systemId2, uint? systemId3, int width, int height, GraphicsResourceType resourceType) : base(0, string.Empty)
        {
            this.graphicsManager = graphicsManager;
            this.GraphicsResourceSystemId = systemId;
            this.GraphicsResourceSystemId2 = systemId2;
            this.GraphicsResourceSystemId3 = systemId3;
            this.Width = width;
            this.Height = height;
            this.ResourceType = resourceType;
            this.IsLoaded = true;
        }

        internal Texture(GraphicsManager graphicsManager, int width, int height, uint resourceId, string path) : base(resourceId, path)
        {
            this.graphicsManager = graphicsManager;
            this.Width = width;
            this.Height = height;
            this.ResourceType = GraphicsResourceType.Static;
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
            set;
        }

        public uint? GraphicsResourceSystemId2
        {
            get;
            set;
        }

        public uint? GraphicsResourceSystemId3
        {
            get;
            set;
        }

        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int MipLevels { get; internal set; }
        public GraphicsResourceType ResourceType { get; }
    }
}