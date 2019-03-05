using System;

namespace CoreEngine
{
    internal class EntityComponentLayoutDesc
    {
        public EntityComponentLayoutDesc(uint entityComponentLayoutId, int hashCode, int componentCount)
        {
            this.EntityComponentLayoutId = entityComponentLayoutId;
            this.HashCode = hashCode;
            this.ComponentCount = componentCount;
            this.ComponentTypes = new int[componentCount];
            this.ComponentOffsets = new int[componentCount];
            this.ComponentSizes = new int[componentCount];
        }

        public uint EntityComponentLayoutId { get; }
        public int HashCode { get;}
        public int Size { get; set; }
        public int ComponentCount { get; }
        public int[] ComponentTypes { get; }
        public int[] ComponentOffsets { get; }
        public int[] ComponentSizes { get; }
    }
}