using System;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;

namespace CoreEngine.Resources
{
    public abstract class ResourceLoader
    {
        protected ResourceLoader(ResourcesManager resourcesManager)
        {
            this.ResourcesManager = resourcesManager;
        }

        public abstract string Name
        {
            get;
        }

        public abstract string FileExtension
        {
            get;
        }

        protected ResourcesManager ResourcesManager
        {
            get;
        }

        public abstract Resource CreateEmptyResource(uint resourceId, string path);
        public abstract Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data);

        public virtual void DestroyResource(Resource resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }
            
            Logger.WriteMessage($"No destroy method for '{resource.GetType()}'...", LogMessageTypes.Warning);
        }
    }
}