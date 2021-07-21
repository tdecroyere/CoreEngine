using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Texture : Resource, IGraphicsResource, IDisposable
    {
        private readonly GraphicsManager graphicsManager;
        private readonly ShaderResourceManager shaderResourceManager;
        private bool isDisposed;

        internal Texture(GraphicsManager graphicsManager, ShaderResourceManager shaderResourceManager, GraphicsMemoryAllocation graphicsMemoryAllocation, GraphicsMemoryAllocation? graphicsMemoryAllocation2, IntPtr nativePointer1, IntPtr? nativePointer2, TextureFormat textureFormat, TextureUsage usage, int width, int height, int faceCount, int mipLevels, int multiSampleCount, bool isStatic, string label) : base(0, string.Empty)
        {
            this.graphicsManager = graphicsManager;
            this.shaderResourceManager = shaderResourceManager;
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
            this.ShaderResourceIndexes1 = new uint[mipLevels];
            this.ShaderResourceIndexes2 = new uint[mipLevels];
            this.WriteableShaderResourceIndex1 = new uint[mipLevels];
            this.WriteableShaderResourceIndex2 = new uint[mipLevels];
        }

        internal Texture(GraphicsManager graphicsManager, ShaderResourceManager shaderResourceManager, int width, int height, uint resourceId, string path, string label) : base(resourceId, path)
        {
            this.graphicsManager = graphicsManager;
            this.shaderResourceManager = shaderResourceManager;
            this.TextureFormat = TextureFormat.Rgba8UnormSrgb;
            this.Width = width;
            this.Height = height;
            this.MultiSampleCount = 1;
            this.ResourceType = GraphicsResourceType.Texture;
            this.IsStatic = true;
            this.Label = label;
            this.ShaderResourceIndexes1 = Array.Empty<uint>();
            this.ShaderResourceIndexes2 = Array.Empty<uint>();
            this.WriteableShaderResourceIndex1 = Array.Empty<uint>();
            this.WriteableShaderResourceIndex2 = Array.Empty<uint>();
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

        // TODO: Refactor the whole API for shader indexes
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

        public uint GetShaderResourceIndex(uint mipLevel)
        {
            if (mipLevel == 0)
            {
                return ShaderResourceIndex;
            }

            // TODO: Check for errors

            if (!IsStatic && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
            {
                var result2 = this.ShaderResourceIndexes2[mipLevel];

                if (result2 == 0)
                {
                    this.shaderResourceManager.CreateShaderResourceTexture(this, isWriteable: false, mipLevel, out var shaderResourceIndex1, out var shaderResourceIndex2);

                    this.ShaderResourceIndexes1[mipLevel] = shaderResourceIndex1;
                    this.ShaderResourceIndexes2[mipLevel] = shaderResourceIndex2!.Value;

                    result2 = shaderResourceIndex2.Value;
                }

                return result2;
            }

            var result = this.ShaderResourceIndexes1[mipLevel];

            if (result == 0)
            {
                this.shaderResourceManager.CreateShaderResourceTexture(this, isWriteable: false, mipLevel, out var shaderResourceIndex1, out var _);

                this.ShaderResourceIndexes1[mipLevel] = shaderResourceIndex1;
                result = shaderResourceIndex1;
            }

            return result;
        }

        internal uint[] ShaderResourceIndexes1 { get; set; }
        internal uint[] ShaderResourceIndexes2 { get; set; }

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

        internal uint[] WriteableShaderResourceIndex1 { get; set; }
        internal uint[] WriteableShaderResourceIndex2 { get; set; }

        public string Label
        {
            get;
        }
    }
}