using System;
using System.Threading.Tasks;

namespace CoreEngine.Resources
{
    public abstract class ResourceStorage
    {
        public abstract string Name
        {
            get;
        }

        public abstract bool IsResourceExists(string path);
        public abstract DateTime? CheckForUpdatedResource(string path, DateTime lastUpdateDateTime);
        public abstract Task<byte[]> ReadResourceDataAsync(string path);
        public abstract string GetResourceFullPath(string path);
    }
}