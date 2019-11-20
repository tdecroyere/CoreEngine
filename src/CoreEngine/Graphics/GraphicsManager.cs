using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsManager : SystemManager
    {
        private readonly IGraphicsService graphicsService;
        private readonly ResourcesManager resourcesManager;

        public GraphicsManager(IGraphicsService graphicsService, ResourcesManager resourcesManager)
        {
            this.graphicsService = graphicsService;
            this.resourcesManager = resourcesManager;

            InitResourceLoaders();
        }

        internal GraphicsBuffer CreateStaticGraphicsBuffer(ReadOnlySpan<byte> data)
        {
            var graphicsBufferId = graphicsService.CreateStaticGraphicsBuffer(data);
            return new GraphicsBuffer(graphicsBufferId, data.Length, GraphicsBufferType.Static);
        }

        internal GraphicsBuffer CreateDynamicGraphicsBuffer(int length)
        {
            var graphicsBufferId = graphicsService.CreateDynamicGraphicsBuffer(length);
            return new GraphicsBuffer(graphicsBufferId, length, GraphicsBufferType.Dynamic);
        }

        private void InitResourceLoaders()
        {
            this.resourcesManager.AddResourceLoader(new ShaderResourceLoader(this.resourcesManager, this.graphicsService));
            this.resourcesManager.AddResourceLoader(new MaterialResourceLoader(this.resourcesManager, this));
            this.resourcesManager.AddResourceLoader(new MeshResourceLoader(this.resourcesManager, this));
        }
    }
}