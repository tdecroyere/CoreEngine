using System;
using System.Collections.Generic;

namespace CoreEngine.Resources
{
    public abstract class Resource
    {
        protected Resource(uint resourceId, string path)
        {
            this.ResourceId = resourceId;
            this.Path = path;
            this.FullPath = path;
            this.Parameters = Array.Empty<string>();
            this.ReferenceCount = 1;
            this.LastUpdateDateTime = DateTime.Now;
            this.DependentResources = new List<Resource>();
        }

        public uint ResourceId
        {
            get;
        }
        
        public string Path
        {
            get;
        }

        public string FullPath
        {
            get;
            internal set;
        }

        public bool IsLoaded
        {
            get;
            internal set;
        }

        public IList<Resource> DependentResources
        {
            get;
        }

        public int ReferenceCount
        {
            get;
            internal set;
        }

        public ResourceLoader? ResourceLoader
        {
            get;
            internal set;
        }

        public Memory<string> Parameters
        {
            get;
            internal set;
        }

        public DateTime LastUpdateDateTime
        {
            get;
            internal set;
        }

        public void DecrementReferenceCount()
        {
            this.ReferenceCount--;
        }

        public virtual void DestroyResource()
        {
            
        }
    }
}