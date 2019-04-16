using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CoreEngine.Resources
{
    public class ResourcesManager : SystemManager
    {
        private IDictionary<string, ResourceLoader> resourceLoaders;
        private IList<ResourceStorage> resourceStorages;

        public ResourcesManager()
        {
            this.resourceLoaders = new Dictionary<string, ResourceLoader>();
            this.resourceStorages = new List<ResourceStorage>();
        }

        public void AddResourceLoader(ResourceLoader resourceLoader)
        {
            Console.WriteLine($"Registering '{resourceLoader.FileExtension}' resource loader...");
            this.resourceLoaders.Add(resourceLoader.FileExtension, resourceLoader);
        }

        public void AddResourceStorage(ResourceStorage resourceStorage)
        {
            Console.WriteLine($"Registering '{resourceStorage.Name}' resource storage...");
            this.resourceStorages.Add(resourceStorage);
        }

        public async Task<Resource> LoadResourceAsync(string path)
        {
            var resourceLoader = FindResourceLoader(Path.GetExtension(path));

            if (resourceLoader == null)
            {
                throw new InvalidOperationException($"Error: No resource loader found '{Path.GetExtension(path)}'.");
            }

            var resourceStorage = FindResourceStorage(path);

            if (resourceStorage == null)
            {
                Console.WriteLine($"Warning: Resource '{path}' was not found.");
                // TODO return a default not found resource specific to the resource type (shader, texture, etc.)
                throw new NotImplementedException();
            }

            var resourceData = await resourceStorage.ReadResourceDataAsync(path);
            var resource = resourceLoader.CreateEmptyResource(path);

            // TODO: Add resource real loading to the queue

            return resource;
        }

        private ResourceLoader? FindResourceLoader(string fileExtension)
        {
            if (this.resourceLoaders.ContainsKey(fileExtension))
            {
                return this.resourceLoaders[fileExtension];
            }

            return null;
        }

        private ResourceStorage? FindResourceStorage(string path)
        {
            for (var i = 0; i < this.resourceStorages.Count; i++)
            {
                if (this.resourceStorages[i].IsResourceExists(path))
                {
                    return this.resourceStorages[i];
                }
            }

            return null;
        }
    }
}