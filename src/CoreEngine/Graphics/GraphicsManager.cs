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
        
        public Shader testShader;

        public GraphicsManager(IGraphicsService graphicsService, ResourcesManager resourcesManager)
        {
            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.graphicsService = graphicsService;
            this.resourcesManager = resourcesManager;

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

        public GraphicsBuffer CreateShaderParameters(Shader shader, GraphicsBuffer graphicsBuffer1, GraphicsBuffer graphicsBuffer2, GraphicsBuffer graphicsBuffer3)
        {
            if (shader == null)
            {
                throw new ArgumentNullException(nameof(shader));
            }

            var graphicsBufferId = this.graphicsService.CreateShaderParameters(shader.PipelineStateId, graphicsBuffer1.SystemId, graphicsBuffer2.SystemId, graphicsBuffer3.SystemId);
            uint? graphicsBufferId2 = null;

            if (graphicsBuffer1.SystemId2 != null && graphicsBuffer2.SystemId2 != null && graphicsBuffer3.SystemId2 != null)
            {
                graphicsBufferId2 = this.graphicsService.CreateShaderParameters(shader.PipelineStateId, graphicsBuffer1.SystemId2.Value, graphicsBuffer2.SystemId2.Value, graphicsBuffer3.SystemId2.Value);
            }

            return new GraphicsBuffer(this, graphicsBufferId, graphicsBufferId2, 0, GraphicsResourceType.Dynamic);
        }

        public GraphicsBuffer CreateGraphicsBuffer(int length, GraphicsResourceType resourceType = GraphicsResourceType.Static)
        {
            var graphicsBufferId = graphicsService.CreateGraphicsBuffer(length);
            uint? graphicsBufferId2 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                graphicsBufferId2 = graphicsService.CreateGraphicsBuffer(length);
            }

            return new GraphicsBuffer(this, graphicsBufferId, graphicsBufferId2, length, resourceType);
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
            var rawData = MemoryMarshal.Cast<T, byte>(data);
            this.graphicsService.UploadDataToGraphicsBuffer(commandList.Id, graphicsBuffer.Id, rawData);
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

        public void SetGraphicsBuffer(CommandList commandList, GraphicsBuffer graphicsBuffer, uint slot)
        {
            var graphicsBufferId = graphicsBuffer.Id;

            this.graphicsService.SetGraphicsBuffer(commandList.Id, graphicsBufferId, GraphicsBindStage.Vertex, slot);
        }

        public void DrawPrimitives(CommandList commandList, GeometryInstance geometryInstance, uint baseInstanceId)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            if (geometryInstance.IndexCount == 0)
            {
                throw new InvalidOperationException("Index count must non-zero.");
            }

            this.graphicsService.DrawPrimitives(commandList.Id, 
                                                (GraphicsPrimitiveType)(int)geometryInstance.PrimitiveType, 
                                                geometryInstance.StartIndex, 
                                                geometryInstance.IndexCount, 
                                                geometryInstance.GeometryPacket.VertexBuffer.Id, 
                                                geometryInstance.GeometryPacket.IndexBuffer.Id, 
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
            this.resourcesManager.AddResourceLoader(new ShaderResourceLoader(this.resourcesManager, this.graphicsService));
            this.resourcesManager.AddResourceLoader(new MaterialResourceLoader(this.resourcesManager, this));
            this.resourcesManager.AddResourceLoader(new MeshResourceLoader(this.resourcesManager, this));
        }
    }
}