using System;
using System.IO;
using System.Threading.Tasks;

namespace CoreEngine.Resources
{
    public class FileSystemResourceStorage : ResourceStorage
    {
        private string basePath;

        public FileSystemResourceStorage(string basePath)
        {
            this.basePath = Path.GetFullPath(basePath);

            // TODO: Check to see if the directory exists
        }

        public override string Name
        {
            get
            {
                return $"FileResourceStorage - BasePath: {this.basePath}";
            }
        }

        public override bool IsResourceExists(string path)
        {
            // TODO: Is this multi os compatible?
            return File.Exists(this.basePath + path);
        }

        public override DateTime? CheckForUpdatedResource(string path, DateTime lastUpdateDateTime)
        {
            if (File.Exists(path))
            {
                var lastWriteTime = File.GetLastWriteTime(this.basePath + path);
                return (lastWriteTime > lastUpdateDateTime) ? (DateTime?)lastWriteTime : null;
            }
            
            return null;
        }

        public override Task<byte[]> ReadResourceDataAsync(string path)
        {
            // TODO: Implement a thread safe queue so that reading on the disk is sequential

            return File.ReadAllBytesAsync(this.basePath + path);
        }
    }
}