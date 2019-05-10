using System;
using System.IO;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class ShaderResourceLoader : ResourceLoader
    {
        private readonly GraphicsService graphicsService;
        private readonly MemoryService memoryService;

        public ShaderResourceLoader(GraphicsService graphicsService, MemoryService memoryService)
        {
            this.graphicsService = graphicsService;
            this.memoryService = memoryService;
        }

        public override string Name => "Shader Loader";
        public override string FileExtension => ".shader";

        public override Resource CreateEmptyResource(string path)
        {
            return new Shader(path);
        }

        public override Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data)
        {
            var shader = resource as Shader;

            if (shader == null)
            {
                throw new ArgumentException("Resource is not a Shader resource.", "resource");
            }

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var shaderSignature = reader.ReadChars(6);
            var shaderVersion = reader.ReadInt32();

            if (shaderSignature.ToString() != "SHADER" && shaderVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong shader signature or version for shader '{resource.Path}'");
            }

            var shaderByteCodeLength = reader.ReadInt32();
            var shaderByteCode = new Span<byte>(reader.ReadBytes(shaderByteCodeLength));

            Logger.WriteMessage("OK Shader loader");

            var shaderByteCodeBuffer = this.memoryService.CreateMemoryBuffer(this.memoryService.MemoryManagerContext, shaderByteCodeLength);
            
            if (!shaderByteCode.TryCopyTo(shaderByteCodeBuffer.AsSpan()))
            {
                Logger.WriteMessage("Shader bytecode copy error");
            }

            Logger.WriteMessage("Shader bytecode copy OK");

            // TODO: Do not forget to implement hardware resource deallocation/reallocation
            
            // TODO: Pass the id here so that the host remove replace the shader himself at the right time
            this.graphicsService.CreateShader(this.graphicsService.GraphicsContext, shaderByteCodeBuffer);

            return Task.FromResult((Resource)shader);
        }
    }
}