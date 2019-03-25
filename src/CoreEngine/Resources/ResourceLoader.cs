using System;

namespace CoreEngine
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
    }
}