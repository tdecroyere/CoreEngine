namespace CoreEngine.Graphics
{
    public readonly struct ShaderParameterDescriptor
    {
        public ShaderParameterDescriptor(IGraphicsResource graphicsResource, ShaderParameterType parameterType, uint slot)
        {
            this.GraphicsResource = graphicsResource;
            this.ParameterType = parameterType;
            this.Slot = slot;
        }

        public readonly IGraphicsResource GraphicsResource { get; }
        public readonly ShaderParameterType ParameterType { get; }
        public readonly uint Slot { get; }
    }
}