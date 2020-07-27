using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Buffers;
using CoreEngine.Diagnostics;
using System.Threading;

namespace CoreEngine.Resources
{
    class ResourceLoadingParameters
    {
        public ResourceLoadingParameters(Resource resource, ResourceStorage resourceStorage)
        {
            this.Resource = resource;
            this.ResourceStorage = resourceStorage;
        }

        public Resource Resource { get; }
        public ResourceStorage ResourceStorage { get; }
    }

    public class ResourcesManager : SystemManager, IDisposable
    {
        private IDictionary<string, ResourceLoader> resourceLoaders;
        private IList<ResourceStorage> resourceStorages;
        private ConcurrentQueue<Task<Resource>> resourceLoadingQueue;
        private IDictionary<string, Resource> resources;
        private IDictionary<uint, Resource> resourceIdList;
        private Task? resourceLoadingRunner;
        private Task resourceUpdateCheckRunner;
        private uint currentResourceId;

        public ResourcesManager()
        {
            this.resourceLoaders = new Dictionary<string, ResourceLoader>();
            this.resourceStorages = new List<ResourceStorage>();
            this.resourceLoadingQueue = new ConcurrentQueue<Task<Resource>>();
            this.resources = new Dictionary<string, Resource>();
            this.resourceIdList = new Dictionary<uint, Resource>();

            this.resourceLoadingRunner = null;

            this.resourceUpdateCheckRunner = CheckForUpdatedResources();
            this.resourceUpdateCheckRunner.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (this.resourceLoadingRunner != null)
                {
                    this.resourceLoadingRunner.Dispose();
                    this.resourceLoadingRunner = null;
                }
            }
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

        private static string ConstructResourceKey(string path, params string[] parameters)
        {
            var resourceKey = path;

            for (var i = 0; i < parameters.Length; i++)
            {
                resourceKey += $"@{parameters[i]}";
            }

            return resourceKey;
        }

        public T LoadResourceAsync<T>(string path, params string[] parameters) where T : Resource
        {
            var resourceKey = ConstructResourceKey(path, parameters);

            if (this.resources.ContainsKey(resourceKey))
            {
                this.resources[resourceKey].ReferenceCount++;
                return (T)this.resources[resourceKey];
            }

            Logger.BeginAction($"Loading resource '{resourceKey}'");
            var resourceLoader = FindResourceLoader(Path.GetExtension(path));

            if (resourceLoader == null)
            {
                throw new InvalidOperationException($"Error: No resource loader found '{Path.GetExtension(path)}'.");
            }

            var resource = resourceLoader.CreateEmptyResource(this.currentResourceId, path);
            resource.ResourceLoader = resourceLoader;
            resource.Parameters = parameters;
            resource.ReferenceCount++;

            if (this.resources.ContainsKey(resourceKey))
            {
                this.resources[resourceKey].ReferenceCount++;
                return (T)this.resources[resourceKey];
            }

            this.resources.Add(resourceKey, resource);
            this.resourceIdList.Add(currentResourceId, resource);
            this.currentResourceId++;

            var resourceStorage = FindResourceStorage(path);

            if (resourceStorage == null)
            {
                Logger.EndActionWarning($"Resource '{path}' was not found.");
                // TODO return a default not found resource specific to the resource type (shader, texture, etc.)
                //throw new NotImplementedException("Resource not found path is not yet implemented");
                resource.IsLoaded = true;
                return (T)resource;
            }

            // var loadingTask = new Task<Resource>(parameters => {
                // var resourceLoadingParameters = parameters as ResourceLoadingParameters;

                // if (resourceLoadingParameters == null)
                // {
                //     throw new ArgumentNullException(nameof(parameters));
                // }

                // var resourceData = resourceLoadingParameters.ResourceStorage.ReadResourceDataAsync(resource.Path).Result;
                // return resource.ResourceLoader.LoadResourceData(resource, resourceData);
            // }, new ResourceLoadingParameters(resource, resourceStorage));

            // this.resourceLoadingQueue.Enqueue(loadingTask);

            Logger.BeginAction($"Reading resource data");
            var resourceData = resourceStorage.ReadResourceDataAsync(resource.Path).Result;
            Logger.EndAction();
            resource = resource.ResourceLoader.LoadResourceData(resource, resourceData);
            resource.IsLoaded = true;
            
            Logger.EndAction();

            return (T)resource;
        }

        public override void PreUpdate()
        {
            // TODO: Deactivated because it took 7 ms !
            
            //CheckResourceLoadingTasks();
            //WaitForPendingResources();
            CheckForUpdatedResources();
            // RemoveUnusedResources();
        }

        public void WaitForPendingResources()
        {
            if (this.resourceLoadingRunner == null)
            {
                CheckResourceLoadingTasks();
            }
            
            this.resourceLoadingRunner?.Wait();
        }

        private void CheckResourceLoadingTasks()
        {
            // TODO: Check if we can run async all the pending loading tasks at the same time.
            // For the moment, it seems that there is a crash because the global heap is creating resources
            // at the same time and at the same location

            if ((this.resourceLoadingRunner == null || this.resourceLoadingRunner.IsCompleted) && this.resourceLoadingQueue.Count > 0)
            {
                this.resourceLoadingRunner = new Task(() => {
                    while (this.resourceLoadingQueue.TryDequeue(out var resourceLoadingTask))
                    {
                        resourceLoadingTask.RunSynchronously();

                        if (resourceLoadingTask.Status == TaskStatus.Faulted)
                        {
                            Logger.WriteMessage("Warning: Failed to load resource");
                            Logger.WriteMessage($"{resourceLoadingTask.Exception}");
                        }

                        var resource = resourceLoadingTask.Result;
                        resource.IsLoaded = true;
                    }
                });

                this.resourceLoadingRunner.Start();
            }
        }
        
        // TODO: This check needs to be done only each seconds and not each frames!
        private Task CheckForUpdatedResources()
        {
            // TODO: Only check for a Maximum of resources for each frame
            // TODO: Try to avoid the copy here
            
            var task = new Task(() => {
                while (true)
                {
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
                                Logger.WriteMessage($"Found update for resource '{item.Key}'...");

                                if (resource.ResourceLoader != null)
                                {
                                    for (var j = 0; j < resource.DependentResources.Count; j++)
                                    {
                                        resource.DependentResources[j].DecrementReferenceCount();
                                    }

                                    resource.DependentResources.Clear();
                                    resource.LastUpdateDateTime = lastUpdateDate.Value;

                                    // var loadingTask = new Task<Resource>(parameters => {
                                    //     var resourceLoadingParameters = parameters as ResourceLoadingParameters;

                                    //     if (resourceLoadingParameters == null)
                                    //     {
                                    //         throw new ArgumentNullException(nameof(parameters));
                                    //     }

                                    //     var resourceData = resourceLoadingParameters.ResourceStorage.ReadResourceDataAsync(resource.Path).Result;
                                    //     return resource.ResourceLoader.LoadResourceData(resource, resourceData);
                                    // }, new ResourceLoadingParameters(resource, this.resourceStorages[i]));


                                    // this.resourceLoadingQueue.Enqueue(loadingTask);

                                    var resourceData = this.resourceStorages[i].ReadResourceDataAsync(resource.Path).Result;
                                    resource = resource.ResourceLoader.LoadResourceData(resource, resourceData);
                                    resource.IsLoaded = true;
                                }
                            }
                        }
                    }

                    Thread.Sleep(1000);
                }
            }, TaskCreationOptions.LongRunning);

            return task;
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