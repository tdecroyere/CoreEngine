using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Texture : Resource, IGraphicsResource
    {
        private readonly GraphicsManager graphicsManager;

        internal Texture(GraphicsManager graphicsManager, uint systemId, uint? systemId2, uint? systemId3, TextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multiSampleCount, GraphicsResourceType resourceType) : base(0, string.Empty)
        {
            this.graphicsManager = graphicsManager;
            this.GraphicsResourceSystemId = systemId;
            this.GraphicsResourceSystemId2 = systemId2;
            this.GraphicsResourceSystemId3 = systemId3;
            this.TextureFormat = textureFormat;
            this.Width = width;
            this.Height = height;
            this.FaceCount = faceCount;
            this.MipLevels = mipLevels;
            this.MultiSampleCount = multiSampleCount;
            this.ResourceType = resourceType;
            this.IsLoaded = true;
        }

        internal Texture(GraphicsManager graphicsManager, int width, int height, uint resourceId, string path) : base(resourceId, path)
        {
            this.graphicsManager = graphicsManager;
            this.TextureFormat = TextureFormat.Rgba8UnormSrgb;
            this.Width = width;
            this.Height = height;
            this.MultiSampleCount = 1;
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

        public TextureFormat TextureFormat { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int FaceCount { get; internal set; }
        public int MipLevels { get; internal set; }
        public int MultiSampleCount { get; internal set; }
        public GraphicsResourceType ResourceType { get; }
    }
}