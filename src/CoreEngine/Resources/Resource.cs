using System;

namespace CoreEngine.Resources
{
    public abstract class Resource
    {
        protected Resource(string path)
        {
            this.Path = path;
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
    }
}