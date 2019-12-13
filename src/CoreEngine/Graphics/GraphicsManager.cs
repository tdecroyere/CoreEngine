using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    // TODO: Implement resource remove functions
    public class GraphicsManager : SystemManager
    {
        private readonly IGraphicsService graphicsService;
        private readonly ResourcesManager resourcesManager;

        private static object syncObject = new object();
        private uint currentGraphicsResourceId;

        public GraphicsManager(IGraphicsService graphicsService, ResourcesManager resourcesManager)
        {
            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.graphicsService = graphicsService;
            this.resourcesManager = resourcesManager;
            this.currentGraphicsResourceId = 0;

            InitResourceLoaders();
        }

        public uint CurrentFrameNumber
        {
            get;
            private set;
        }

        public Vector2 GetRenderSize()
        {
            return this.graphicsService.GetRenderSize();
        }

        public GraphicsBuffer CreateGraphicsBuffer<T>(int length, GraphicsResourceType resourceType = GraphicsResourceType.Static, string? debugName = null) where T : struct
        {
            var sizeInBytes = Marshal.SizeOf(typeof(T)) * length;

            var graphicsBufferId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId, sizeInBytes, debugName);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
            }

            uint? graphicsBufferId2 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                graphicsBufferId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId2.Value, sizeInBytes, debugName);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
                }
            }

            return new GraphicsBuffer(this, graphicsBufferId, graphicsBufferId2, sizeInBytes, resourceType);
        }

        // TODO: Add additional parameters (format, depth, mipLevels, etc.<)
        public Texture CreateTexture(int width, int height, GraphicsResourceType resourceType = GraphicsResourceType.Static, string? debugName = null)
        {
            var textureId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateTexture(textureId, width, height, debugName);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the texture resource.");
            }

            uint? textureId2 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                textureId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateTexture(textureId2.Value, width, height, debugName);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the texture resource.");
                }
            }

            return new Texture(this, textureId, textureId2, width, height, resourceType);
        }

        internal Shader CreateShader(ReadOnlySpan<byte> shaderByteCode, string? debugName = null)
        {
            var shaderId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateShader(shaderId, shaderByteCode, debugName);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the shader resource.");
            }

            return new Shader(shaderId);
        }

        internal void RemoveShader(Shader shader)
        {
            this.graphicsService.RemoveShader(shader.ShaderId);
        }

        public CommandList CreateCopyCommandList(string? debugName = null, bool createNewCommandBuffer = false)
        {
            var commandListId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateCopyCommandList(commandListId, debugName, createNewCommandBuffer);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the copy command list resource.");
            }

            return new CommandList(commandListId, CommandListType.Copy);
        }

        public void ExecuteCopyCommandList(CommandList commandList)
        {
            if (commandList.Type != CommandListType.Copy)
            {
                throw new InvalidOperationException("The specified command list is not a copy command list.");
            }

            this.graphicsService.ExecuteCopyCommandList(commandList.Id);
        }

        public void UploadDataToGraphicsBuffer<T>(CommandList commandList, GraphicsBuffer graphicsBuffer, ReadOnlySpan<T> data) where T : struct
        {
            // TODO: Do something for memory alignement of data in the shaders?
            var rawData = MemoryMarshal.Cast<T, byte>(data);
            this.graphicsService.UploadDataToGraphicsBuffer(commandList.Id, graphicsBuffer.GraphicsResourceId, rawData);
        }

        public void UploadDataToTexture<T>(CommandList commandList, Texture texture, ReadOnlySpan<T> data) where T : struct
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            var rawData = MemoryMarshal.Cast<T, byte>(data);
            this.graphicsService.UploadDataToTexture(commandList.Id, texture.GraphicsResourceId, texture.Width, texture.Height, rawData);
        }

        public CommandList CreateRenderCommandList(string? debugName = null, bool createNewCommandBuffer = false)
        {
            var commandListId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateRenderCommandList(commandListId, debugName, createNewCommandBuffer);

            // if (!result)
            // {
            //     throw new InvalidOperationException("There was an error while creating the render command list resource.");
            // }

            return new CommandList(commandListId, CommandListType.Render);
        }

        public void ExecuteRenderCommandList(CommandList commandList)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.ExecuteRenderCommandList(commandList.Id);
        }

        public void SetShader(CommandList commandList, Shader shader)
        {
            if (shader == null)
            {
                throw new ArgumentNullException(nameof(shader));
            }

            this.graphicsService.SetShader(commandList.Id, shader.ShaderId);
        }

        public void SetShaderBuffer(CommandList commandList, GraphicsBuffer graphicsBuffer, int slot, int index = 0)
        {
            this.graphicsService.SetShaderBuffer(commandList.Id, graphicsBuffer.GraphicsResourceId, slot, index);
        }

        public void SetShaderBuffers(CommandList commandList, ReadOnlySpan<GraphicsBuffer> graphicsBuffers, int slot, int index = 0)
        {
            if (graphicsBuffers == null)
            {
                throw new ArgumentNullException(nameof(graphicsBuffers));
            }

            var graphicsBufferIdsList = new uint[graphicsBuffers.Length];

            for (var i = 0; i < graphicsBuffers.Length; i++)
            {
                graphicsBufferIdsList[i] = graphicsBuffers[i].GraphicsResourceId;
            }

            this.graphicsService.SetShaderBuffers(commandList.Id, graphicsBufferIdsList.AsSpan(), slot, index);
        }

        public void SetShaderTexture(CommandList commandList, Texture texture, int slot, int index = 0)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            this.graphicsService.SetShaderTexture(commandList.Id, texture.GraphicsResourceId, slot, index);
        }

        public void SetShaderTextures(CommandList commandList, ReadOnlySpan<Texture> textures, int slot, int index = 0)
        {
            if (textures == null)
            {
                throw new ArgumentNullException(nameof(textures));
            }

            var textureIdsList = new uint[textures.Length];

            for (var i = 0; i < textures.Length; i++)
            {
                textureIdsList[i] = textures[i].GraphicsResourceId;
            }

            this.graphicsService.SetShaderTextures(commandList.Id, textureIdsList.AsSpan(), slot, index);
        }

        public void SetIndexBuffer(CommandList commandList, GraphicsBuffer indexBuffer)
        {
            this.graphicsService.SetIndexBuffer(commandList.Id, indexBuffer.GraphicsResourceId);
        }

        public void DrawGeometryInstances(CommandList commandList, GeometryInstance geometryInstance, int instanceCount, int baseInstanceId)
        {
            if (geometryInstance.IndexCount == 0)
            {
                throw new InvalidOperationException("Index count must non-zero.");
            }

            this.SetShaderBuffer(commandList, geometryInstance.GeometryPacket.VertexBuffer, 0);
            this.SetIndexBuffer(commandList, geometryInstance.GeometryPacket.IndexBuffer);

            this.DrawIndexedPrimitives(commandList, 
                                        geometryInstance.PrimitiveType, 
                                        geometryInstance.StartIndex, 
                                        geometryInstance.IndexCount, 
                                        instanceCount,
                                        baseInstanceId);
        }

        public void DrawIndexedPrimitives(CommandList commandList, GeometryPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.DrawIndexedPrimitives(commandList.Id, 
                                                (GraphicsPrimitiveType)(int)primitiveType, 
                                                startIndex, 
                                                indexCount,
                                                instanceCount,
                                                baseInstanceId);
        }

        public void PresentScreenBuffer()
        {
            this.graphicsService.PresentScreenBuffer();

            // TODO: A modulo here with Int.MaxValue
            this.CurrentFrameNumber++;
        }

        private void InitResourceLoaders()
        {
            this.resourcesManager.AddResourceLoader(new TextureResourceLoader(this.resourcesManager, this));
            this.resourcesManager.AddResourceLoader(new ShaderResourceLoader(this.resourcesManager, this));
            this.resourcesManager.AddResourceLoader(new MaterialResourceLoader(this.resourcesManager, this));
            this.resourcesManager.AddResourceLoader(new MeshResourceLoader(this.resourcesManager, this));
        }

        internal uint GetNextGraphicsResourceId()
        {
            uint result = 0;

            lock (syncObject)
            {
                result = ++this.currentGraphicsResourceId;
            }

            return result;
        }
    }
}