using System;
using System.Threading.Tasks;

namespace CoreEngine.Resources
{
    public abstract class ResourceLoader
    {
        public abstract string Name
        {
            get;
        }

        public abstract string FileExtension
        {
            get;
        }

        public abstract Resource CreateEmptyResource(string path);
        public abstract Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data);
    }
}