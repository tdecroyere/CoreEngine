using System;
using System.IO;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    // TODO: Add pipeline input descriptors to bind to DirectX12 and Metal
    
    public class ShaderResourceLoader : ResourceLoader
    {
        private readonly IGraphicsService graphicsService;

        public ShaderResourceLoader(ResourcesManager resourcesManager, IGraphicsService graphicsService) : base(resourcesManager)
        {
            this.graphicsService = graphicsService;
        }

        public override string Name => "Shader Loader";
        public override string FileExtension => ".shader";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            return new Shader(resourceId, path);
        }

        public override Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data)
        {
            var shader = resource as Shader;

            if (shader == null)
            {
                throw new ArgumentException("Resource is not a Shader resource.", nameof(resource));
            }

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var shaderSignature = reader.ReadChars(6);
            var shaderVersion = reader.ReadInt32();

            if (shaderSignature.ToString() != "SHADER" && shaderVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for shader '{resource.Path}'");
                return Task.FromResult(resource);
            }

            var shaderByteCodeLength = reader.ReadInt32();
            var shaderByteCode = reader.ReadBytes(shaderByteCodeLength);

            var shaderId = this.graphicsService.CreateShader(shaderByteCode);
            shader.ShaderId = shaderId;

            return Task.FromResult((Resource)shader);
        }
    }
}