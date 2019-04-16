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
    }
}