using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Buffers;

namespace CoreEngine.Resources
{
    public class ResourcesManager : SystemManager
    {
        private IDictionary<string, ResourceLoader> resourceLoaders;
        private IList<ResourceStorage> resourceStorages;
        private IList<Task<Resource>> resourceLoadingList;
        private IDictionary<string, Resource> resources;

        public ResourcesManager()
        {
            this.resourceLoaders = new Dictionary<string, ResourceLoader>();
            this.resourceStorages = new List<ResourceStorage>();
            this.resourceLoadingList = new List<Task<Resource>>();
            this.resources = new Dictionary<string, Resource>();
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

        public T LoadResourceAsync<T>(string path) where T : Resource
        {
            if (this.resources.ContainsKey(path))
            {
                return (T)this.resources[path];
            }

            Console.WriteLine($"Loading resource '{path}'...");
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
                throw new NotImplementedException("Resource not found path is not yet implemented");
            }

            // TODO: Implement data handling with stream or just byte array
            // TODO: Move disk data reading in the loading method impl from the update method

            var resourceData = resourceStorage.ReadResourceDataAsync(path).Result;
            var resource = resourceLoader.CreateEmptyResource(path);

            // TODO: Add support for children hierarchical resource loading

            var resourceLoadingTask = resourceLoader.LoadResourceDataAsync(resource, resourceData);
            this.resourceLoadingList.Add(resourceLoadingTask);

            this.resources.Add(path, resource);

            return (T)resource;
        }

        public override void Update()
        {
            // TODO: Add resource finalization for hardware dependent resources?
            // TODO: Add notification system to parent waiting for children resources to load?
            // TODO: Process a fixed amound of resources per frame for now

            var tasksToRemove = ArrayPool<Task<Resource>>.Shared.Rent(10);

            for (var i = 0; i < this.resourceLoadingList.Count; i++)
            {
                var resourceLoadingTask = this.resourceLoadingList[i];

                if (resourceLoadingTask.Status == TaskStatus.Faulted)
                {
                    // TODO: Add more logging infos
                    Console.WriteLine("Warning: Failed to load resource");
                }

                var resource = resourceLoadingTask.Result;
                resource.IsLoaded = true;

                resourceLoadingTask.Dispose();
                tasksToRemove[i] = resourceLoadingTask;
            }

            for (var i = 0; i < tasksToRemove.Length; i++)
            {
                resourceLoadingList.Remove(tasksToRemove[i]);
            }

            ArrayPool<Task<Resource>>.Shared.Return(tasksToRemove);
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