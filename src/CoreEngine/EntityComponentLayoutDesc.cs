using System;

namespace CoreEngine
{
    internal struct EntityComponentLayoutDesc
    {
        public uint EntityComponentLayoutId;
        public int HashCode;
        public int Size;
        public int ComponentCount;
        public int[] ComponentTypes;
        public int[] ComponentOffsets;
        public int[] ComponentSizes;
    }
}