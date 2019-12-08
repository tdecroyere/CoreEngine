using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Texture : Resource
    {
        private readonly GraphicsManager graphicsManager;

        internal Texture(GraphicsManager graphicsManager, uint systemId, uint? systemId2, int width, int height, GraphicsResourceType resourceType) : base(0, string.Empty)
        {
            this.graphicsManager = graphicsManager;
            this.SystemId = systemId;
            this.SystemId2 = systemId2;
            this.Width = width;
            this.Height = height;
            this.ResourceType = resourceType;
        }

        internal Texture(GraphicsManager graphicsManager, int width, int height, uint resourceId, string path) : base(resourceId, path)
        {
            this.graphicsManager = graphicsManager;
            this.Width = width;
            this.Height = height;
            this.ResourceType = GraphicsResourceType.Static;
        }

        public uint TextureId 
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

        internal uint SystemId
        {
            get;
            set;
        }

        internal uint? SystemId2
        {
            get;
            set;
        }

        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public GraphicsResourceType ResourceType { get; }
    }
}