using System;

namespace CoreEngine.Resources
{
    public abstract class Resource
    {
        protected Resource(uint resourceId, string path)
        {
            this.ResourceId = resourceId;
            this.Path = path;
            this.LastUpdateDateTime = DateTime.Now;
        }

        public uint ResourceId
        {
            get;
        }
        
        public string Path
        {
            get;
        }

        public bool IsLoaded
        {
            get;
            internal set;
        }

        public ResourceLoader? ResourceLoader
        {
            get;
            internal set;
        }

        public DateTime LastUpdateDateTime
        {
            get;
            internal set;
        }
    }
}