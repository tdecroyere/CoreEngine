using System;
using System.IO;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class MaterialResourceLoader : ResourceLoader
    {
        private readonly GraphicsManager graphicsManager;
        private readonly MemoryService memoryService;

        public MaterialResourceLoader(ResourcesManager resourcesManager, GraphicsManager graphicsManager, MemoryService memoryService) : base(resourcesManager)
        {
            this.graphicsManager = graphicsManager;
            this.memoryService = memoryService;
        }

        public override string Name => "Material Loader";
        public override string FileExtension => ".material";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            return new Material(resourceId, path);
        }

        public override Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data)
        {
            return Task.FromResult((Resource)resource);
        }
    }
}