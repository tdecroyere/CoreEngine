using System.Collections.Generic;

namespace CoreEngine.Graphics
{
    public readonly struct ShaderParameterDescriptor
    {
        public ShaderParameterDescriptor(IGraphicsResource graphicsResource, ShaderParameterType parameterType, uint slot) : this(new IGraphicsResource[] {graphicsResource}, parameterType, slot)
        {

        }

        public ShaderParameterDescriptor(IGraphicsResource[] graphicsResourceList, ShaderParameterType parameterType, uint slot)
        {
            this.GraphicsResourceList = graphicsResourceList;
            this.ParameterType = parameterType;
            this.Slot = slot;
        }

        public readonly IList<IGraphicsResource> GraphicsResourceList { get; }
        public readonly ShaderParameterType ParameterType { get; }
        public readonly uint Slot { get; }
    }
}