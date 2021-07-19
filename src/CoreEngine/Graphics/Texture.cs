using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Texture : Resource, IGraphicsResource, IDisposable
    {
        private readonly GraphicsManager graphicsManager;
        private bool isDisposed;

        internal Texture(GraphicsManager graphicsManager, GraphicsMemoryAllocation graphicsMemoryAllocation, GraphicsMemoryAllocation? graphicsMemoryAllocation2, IntPtr nativePointer1, IntPtr? nativePointer2, TextureFormat textureFormat, TextureUsage usage, int width, int height, int faceCount, int mipLevels, int multiSampleCount, bool isStatic, string label) : base(0, string.Empty)
        {
            this.graphicsManager = graphicsManager;
            this.GraphicsMemoryAllocation = graphicsMemoryAllocation;
            this.GraphicsMemoryAllocation2 = graphicsMemoryAllocation2;
            this.NativePointer1 = nativePointer1;
            this.NativePointer2 = nativePointer2;
            this.TextureFormat = textureFormat;
            this.Usage = usage;
            this.Width = width;
            this.Height = height;
            this.FaceCount = faceCount;
            this.MipLevels = mipLevels;
            this.MultiSampleCount = multiSampleCount;
            this.ResourceType = GraphicsResourceType.Texture;
            this.IsStatic = isStatic;
            this.IsLoaded = true;
            this.Label = label;
            this.WriteableShaderResourceIndex1 = new uint[mipLevels];
            this.WriteableShaderResourceIndex2 = new uint[mipLevels];
        }

        internal Texture(GraphicsManager graphicsManager, int width, int height, uint resourceId, string path, string label) : base(resourceId, path)
        {
            this.graphicsManager = graphicsManager;
            this.TextureFormat = TextureFormat.Rgba8UnormSrgb;
            this.Width = width;
            this.Height = height;
            this.MultiSampleCount = 1;
            this.ResourceType = GraphicsResourceType.Texture;
            this.IsStatic = true;
            this.Label = label;
            this.WriteableShaderResourceIndex1 = new uint[0];
            this.WriteableShaderResourceIndex2 = new uint[0];
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing && !this.isDisposed)
            {
                this.graphicsManager.ScheduleDeleteTexture(this);
                this.isDisposed = true;
            }
        }

        public IntPtr NativePointer 
        { 
            get
            {
                var result = this.NativePointer1;

                if (!IsStatic && this.NativePointer2 != null && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
                {
                    result = this.NativePointer2.Value;
                }

                return result;
            }
        }

        public IntPtr NativePointer1
        {
            get;
            set;
        }

        public IntPtr? NativePointer2
        {
            get;
            set;
        }

        public TextureFormat TextureFormat { get; internal set; }
        public TextureUsage Usage { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int FaceCount { get; internal set; }
        public int MipLevels { get; internal set; }
        public int MultiSampleCount { get; internal set; }
        public GraphicsResourceType ResourceType { get; }
        public bool IsStatic { get; }
        public GraphicsMemoryAllocation GraphicsMemoryAllocation { get; }
        public GraphicsMemoryAllocation? GraphicsMemoryAllocation2 { get; }

        public uint ShaderResourceIndex 
        { 
            get
            {
                var result = this.ShaderResourceIndex1;

                if (!IsStatic && this.ShaderResourceIndex2 != null && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
                {
                    result = this.ShaderResourceIndex2.Value;
                }

                return result;
            }
        }

        public uint ShaderResourceIndex1 { get; internal set; }
        public uint? ShaderResourceIndex2 { get; internal set; }

        public uint GetWriteableShaderResourceIndex(uint mipLevel)
        {
            // TODO: Check for errors

            var result = this.WriteableShaderResourceIndex1[mipLevel];

            if (!IsStatic && this.WriteableShaderResourceIndex2[mipLevel] != 0 && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
            {
                result = this.WriteableShaderResourceIndex2[mipLevel];
            }

            return result;
        }

        public uint[] WriteableShaderResourceIndex1 { get; internal set; }
        public uint[] WriteableShaderResourceIndex2 { get; internal set; }

        public string Label
        {
            get;
        }
    }
}