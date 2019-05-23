using System;
using CoreEngine.Resources;

namespace CoreEngine
{
    public class Scene : Resource
    {
        public Scene() : base(0, string.Empty)
        {
            this.EntityManager = new EntityManager();
        }

        internal Scene(uint resourceId, string path) : base(resourceId, path)
        {
            this.EntityManager = new EntityManager();
        }

        public EntityManager EntityManager { get; }
    }
}