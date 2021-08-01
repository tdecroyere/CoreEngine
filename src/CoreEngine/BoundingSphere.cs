namespace CoreEngine
{
    public readonly record struct BoundingSphere
    {
        public BoundingSphere(Vector3 center, float radius)
        {
            this.Center = center;
            this.Radius = radius; 
        }

        public Vector3 Center { get; }
        public float Radius { get; }
    }
}