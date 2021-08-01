namespace CoreEngine
{
    public class Scene : Resource
    {
        public Scene() : base(0, string.Empty)
        {
            this.EntityManager = new EntityManager();
            this.EntitySystemManager = new EntitySystemManager();
        }

        internal Scene(uint resourceId, string path) : base(resourceId, path)
        {
            this.EntityManager = new EntityManager();
            this.EntitySystemManager = new EntitySystemManager();
        }

        public EntityManager EntityManager { get; internal set; }
        public EntitySystemManager EntitySystemManager { get; internal set; }

        public void Reset()
        {
            this.EntityManager = new EntityManager();
        }
    }
}