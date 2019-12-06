using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Buffers;
using CoreEngine.Diagnostics;

namespace CoreEngine.Resources
{
    public class ResourcesManager : SystemManager
    {
        private IDictionary<string, ResourceLoader> resourceLoaders;
        private IList<ResourceStorage> resourceStorages;
        private IList<Task<Resource>> resourceLoadingList;
        private IDictionary<string, Resource> resources;
        private IDictionary<uint, Resource> resourceIdList;
        private uint currentResourceId;

        public ResourcesManager()
        {
            this.resourceLoaders = new Dictionary<string, ResourceLoader>();
            this.resourceStorages = new List<ResourceStorage>();
            this.resourceLoadingList = new List<Task<Resource>>();
            this.resources = new Dictionary<string, Resource>();
            this.resourceIdList = new Dictionary<uint, Resource>();
        }

        public void AddResourceLoader(ResourceLoader resourceLoader)
        {
            if (resourceLoader == null)
            {
                throw new ArgumentNullException(nameof(resourceLoader));
            }

            Logger.WriteMessage($"Registering '{resourceLoader.FileExtension}' resource loader...");
            this.resourceLoaders.Add(resourceLoader.FileExtension, resourceLoader);
        }

        public void AddResourceStorage(ResourceStorage resourceStorage)
        {
            if (resourceStorage == null)
            {
                throw new ArgumentNullException(nameof(resourceStorage));
            }

            Logger.WriteMessage($"Registering '{resourceStorage.Name}' resource storage...");
            this.resourceStorages.Add(resourceStorage);
        }

        public T GetResourceById<T>(uint resourceId) where T : Resource
        {
            if (!this.resourceIdList.ContainsKey(resourceId))
            {
                throw new ArgumentException($"No resource with id: '{resourceId}' exists.");
            }

            return (T)this.resourceIdList[resourceId];
        }

        public T LoadResourceAsync<T>(string path) where T : Resource
        {
            if (this.resources.ContainsKey(path))
            {
                this.resources[path].ReferenceCount++;
                return (T)this.resources[path];
            }

            Logger.BeginAction($"Loading resource '{path}'");
            var resourceLoader = FindResourceLoader(Path.GetExtension(path));

            if (resourceLoader == null)
            {
                throw new InvalidOperationException($"Error: No resource loader found '{Path.GetExtension(path)}'.");
            }

            var resource = resourceLoader.CreateEmptyResource(this.currentResourceId, path);
            resource.ResourceLoader = resourceLoader;

            this.resources.Add(path, resource);
            this.resourceIdList.Add(currentResourceId, resource);

            this.currentResourceId++;
            // TODO: Current resource ID is not thread-safe

            var resourceStorage = FindResourceStorage(path);

            if (resourceStorage == null)
            {
                Logger.EndActionWarning($"Resource '{path}' was not found.");
                // TODO return a default not found resource specific to the resource type (shader, texture, etc.)
                //throw new NotImplementedException("Resource not found path is not yet implemented");

                return (T)resource;
            }

            // TODO: Implement data handling with stream or just byte array
            // TODO: Move disk data reading in the loading method impl from the update method

            var resourceData = resourceStorage.ReadResourceDataAsync(path).Result;

            // TODO: Add support for children hierarchical resource loading

            var resourceLoadingTask = resourceLoader.LoadResourceDataAsync(resource, resourceData);
            this.resourceLoadingList.Add(resourceLoadingTask);

            Logger.EndAction();

            return (T)resource;
        }

        public override void PreUpdate()
        {
            CheckResourceLoadingTasks();
            CheckForUpdatedResources();
            RemoveUnusedResources();
        }
        
        // TODO: This check needs to be done only each seconds and not each frames!
        private void CheckForUpdatedResources()
        {
            // TODO: Make that a background task on another thread?
            // TODO: Try to avoid the copy?
            var resourcesSnapshot = new KeyValuePair<string, Resource>[this.resources.Count];
            this.resources.CopyTo(resourcesSnapshot, 0);

            foreach (var item in resourcesSnapshot)
            {
                var resource = item.Value;

                for (var i = 0; i < this.resourceStorages.Count; i++)
                {
                    var lastUpdateDate = this.resourceStorages[i].CheckForUpdatedResource(resource.Path, resource.LastUpdateDateTime);

                    if (lastUpdateDate != null)
                    {
                        Logger.WriteMessage($"Found update for resource '{resource.Path}'...");

                        if (resource.ResourceLoader != null)
                        {
                            for (var j = 0; j < resource.DependentResources.Count; j++)
                            {
                                resource.DependentResources[j].DecrementReferenceCount();
                            }

                            resource.DependentResources.Clear();

                            resource.LastUpdateDateTime = lastUpdateDate.Value;
                            var resourceData = this.resourceStorages[i].ReadResourceDataAsync(resource.Path).Result;

                            var resourceLoadingTask = resource.ResourceLoader.LoadResourceDataAsync(resource, resourceData);
                            this.resourceLoadingList.Add(resourceLoadingTask);
                        }
                    }
                }
            }
        }

        private void CheckResourceLoadingTasks()
        {
            // TODO: Add resource finalization for hardware dependent resources?
            // TODO: Add notification system to parent waiting for children resources to load?
            // TODO: Process a fixed amound of resources per frame for now
            var maxResourceLoadingTask = 10;

            var tasksToRemove = ArrayPool<Task<Resource>>.Shared.Rent(maxResourceLoadingTask);

            for (var i = 0; i < tasksToRemove.Length && i < this.resourceLoadingList.Count; i++)
            {
                var resourceLoadingTask = this.resourceLoadingList[i];

                if (resourceLoadingTask.Status == TaskStatus.Faulted)
                {
                    // TODO: Add more logging infos
                    Logger.WriteMessage("Warning: Failed to load resource");
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

        private void RemoveUnusedResources()
        {
            var resourcesToRemove = ArrayPool<Resource>.Shared.Rent(10);

            // TODO: Try to avoid the copy?
            var resourcesSnapshot = new KeyValuePair<string, Resource>[this.resources.Count];
            this.resources.CopyTo(resourcesSnapshot, 0);

            foreach (var item in resourcesSnapshot)
            {
                if (item.Value.ReferenceCount == 0)
                {
                    Logger.WriteMessage($"Removing resource {item.Value.ResourceId}");

                    if (item.Value.ResourceLoader != null)
                    {
                        item.Value.ResourceLoader!.DestroyResource(item.Value);
                        this.resources.Remove(item.Key);
                    }
                }
            }
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