using System;
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
        
        public Shader testShader;

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

            this.testShader = resourcesManager.LoadResourceAsync<Shader>("/BasicRender.shader");
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

        // TODO: Allow the creation of linked graphics buffers from a struct?
        // TODO: Allow the specification of dynamic or static?
        // TODO: Don't return a GraphicsBuffer but a specific struct
        public GraphicsBuffer CreateShaderParameters(Shader shader, ReadOnlySpan<ShaderParameterDescriptor> parameterDescriptors)
        {
            if (shader == null)
            {
                throw new ArgumentNullException(nameof(shader));
            }

            var nativeParameterDescriptors = new GraphicsShaderParameterDescriptor[parameterDescriptors.Length];

            for (var i = 0; i < parameterDescriptors.Length; i++)
            {
                nativeParameterDescriptors[i] = new GraphicsShaderParameterDescriptor(parameterDescriptors[i].GraphicsResource.SystemId, (GraphicsShaderParameterType)(int)parameterDescriptors[i].ParameterType, parameterDescriptors[i].Slot);
            }

            var graphicsBufferId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateShaderParameters(graphicsBufferId, shader.PipelineStateId, nativeParameterDescriptors);
            
            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
            }
            
            var graphicsBufferId2 = GetNextGraphicsResourceId();

            nativeParameterDescriptors = new GraphicsShaderParameterDescriptor[parameterDescriptors.Length];

            for (var i = 0; i < parameterDescriptors.Length; i++)
            {
                var parameterDescriptor = parameterDescriptors[i];
                var resourceId = parameterDescriptor.GraphicsResource.SystemId2 != null ? parameterDescriptor.GraphicsResource.SystemId2.Value : parameterDescriptor.GraphicsResource.SystemId;

                nativeParameterDescriptors[i] = new GraphicsShaderParameterDescriptor(resourceId, (GraphicsShaderParameterType)(int)parameterDescriptor.ParameterType, parameterDescriptor.Slot);
            }

            result = this.graphicsService.CreateShaderParameters(graphicsBufferId2, shader.PipelineStateId, nativeParameterDescriptors);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
            }

            return new GraphicsBuffer(this, graphicsBufferId, graphicsBufferId2, 0, GraphicsResourceType.Dynamic);
        }

        public GraphicsBuffer CreateGraphicsBuffer<T>(int length, GraphicsResourceType resourceType = GraphicsResourceType.Static) where T : struct
        {
            var sizeInBytes = Marshal.SizeOf(typeof(T)) * length;

            var graphicsBufferId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateGraphicsBuffer(graphicsBufferId, sizeInBytes);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
            }

            uint? graphicsBufferId2 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                graphicsBufferId2 = GetNextGraphicsResourceId();
                result = graphicsService.CreateGraphicsBuffer(graphicsBufferId2.Value, sizeInBytes);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
                }
            }

            return new GraphicsBuffer(this, graphicsBufferId, graphicsBufferId2, sizeInBytes, resourceType);
        }

        // TODO: Add additional parameters (format, depth, mipLevels, etc.<)
        public Texture CreateTexture(int width, int height, GraphicsResourceType resourceType = GraphicsResourceType.Static)
        {
            var textureId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateTexture(textureId, width, height);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the texture resource.");
            }

            uint? textureId2 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                textureId2 = GetNextGraphicsResourceId();
                result = graphicsService.CreateTexture(textureId2.Value, width, height);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the texture resource.");
                }
            }

            return new Texture(this, textureId, textureId2, width, height, resourceType);
        }

        public CommandList CreateCopyCommandList()
        {
            var commandListId = graphicsService.CreateCopyCommandList();
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
            this.graphicsService.UploadDataToGraphicsBuffer(commandList.Id, graphicsBuffer.Id, rawData);
        }

        public void UploadDataToTexture<T>(CommandList commandList, Texture texture, ReadOnlySpan<T> data) where T : struct
        {
            var rawData = MemoryMarshal.Cast<T, byte>(data);
            this.graphicsService.UploadDataToTexture(commandList.Id, texture.Id, texture.Width, texture.Height, rawData);
        }

        public CommandList CreateRenderCommandList()
        {
            var commandListId = graphicsService.CreateRenderCommandList();
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

            this.graphicsService.SetPipelineState(commandList.Id, shader.PipelineStateId);
        }

        public void SetGraphicsBuffer(CommandList commandList, GraphicsBuffer graphicsBuffer, GraphicsBindStage bindStage, uint slot)
        {
            this.graphicsService.SetGraphicsBuffer(commandList.Id, graphicsBuffer.Id, (CoreEngine.HostServices.GraphicsBindStage)(int)bindStage, slot);
        }

        public void SetTexture(CommandList commandList, Texture texture, GraphicsBindStage bindStage, uint slot)
        {
            this.graphicsService.SetTexture(commandList.Id, texture.Id, (CoreEngine.HostServices.GraphicsBindStage)(int)bindStage, slot);
        }

        public void DrawGeometryInstances(CommandList commandList, GeometryInstance geometryInstance, int instanceCount, int baseInstanceId)
        {
            if (geometryInstance.IndexCount == 0)
            {
                throw new InvalidOperationException("Index count must non-zero.");
            }

            this.DrawPrimitives(commandList, 
                                geometryInstance.PrimitiveType, 
                                geometryInstance.StartIndex, 
                                geometryInstance.IndexCount, 
                                geometryInstance.GeometryPacket.VertexBuffer, 
                                geometryInstance.GeometryPacket.IndexBuffer, 
                                instanceCount,
                                baseInstanceId);
        }

        public void DrawPrimitives(CommandList commandList, GeometryPrimitiveType primitiveType, int startIndex, int indexCount, GraphicsBuffer vertexBuffer, GraphicsBuffer indexBuffer, int instanceCount, int baseInstanceId)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.DrawPrimitives(commandList.Id, 
                                                (GraphicsPrimitiveType)(int)primitiveType, 
                                                startIndex, 
                                                indexCount, 
                                                vertexBuffer.Id, 
                                                indexBuffer.Id, 
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
            this.resourcesManager.AddResourceLoader(new ShaderResourceLoader(this.resourcesManager, this.graphicsService));
            this.resourcesManager.AddResourceLoader(new MaterialResourceLoader(this.resourcesManager, this));
            this.resourcesManager.AddResourceLoader(new MeshResourceLoader(this.resourcesManager, this));
        }

        private uint GetNextGraphicsResourceId()
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