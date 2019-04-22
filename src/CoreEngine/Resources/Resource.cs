using System;

namespace CoreEngine.Resources
{
    public abstract class Resource
    {
        protected Resource(string path)
        {
            this.Path = path;
            this.LastUpdateDateTime = DateTime.Now;
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

        public ResourceLoader ResourceLoader
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