namespace CoreEngine
{
    public readonly struct EntityComponentLayout
    {
        public EntityComponentLayout(uint id)
        {
            this.EntityComponentLayoutId = id;
        }
        
        public readonly uint EntityComponentLayoutId { get; }
    }
}