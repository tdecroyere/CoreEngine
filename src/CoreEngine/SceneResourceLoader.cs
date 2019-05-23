using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine
{
    public class SceneResourceLoader : ResourceLoader
    {
        public SceneResourceLoader()
        {

        }

        public override string Name => "Scene Loader";
        public override string FileExtension => ".scene";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            return new Scene(resourceId, path);
        }

        public override Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data)
        {
            var scene = resource as Scene;

            if (scene == null)
            {
                throw new ArgumentException("Resource is not a Scene resource.", "resource");
            }

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var meshSignature = reader.ReadChars(5);
            var meshVersion = reader.ReadInt32();

            if (meshSignature.ToString() != "SCENE" && meshVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for scene '{resource.Path}'");
                return Task.FromResult(resource);
            }

            Logger.WriteMessage("Scene Loading");

            var entityLayoutsCount = reader.ReadInt32();
            var entitiesCount = reader.ReadInt32();

            Logger.WriteMessage($"Loading {entitiesCount} entities (Layouts: {entityLayoutsCount})");

            for (var i = 0; i < entityLayoutsCount; i++)
            {
                var typesCount = reader.ReadInt32();

                for (var j = 0; j < typesCount; j++)
                {
                    var typeFullName = reader.ReadString();
                    var type = FindType(typeFullName);
                    Logger.WriteMessage($"Found Type: {type}");
                }
            }

            return Task.FromResult((Resource)scene);
        }

        private static Type? FindType(string fullTypeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                var type = assembly.GetType(fullTypeName);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}