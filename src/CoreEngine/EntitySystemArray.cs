using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    // TODO: Implement a readonly version
    public class EntitySystemArray<T> where T : struct
    {
        private IList<Memory<byte>> memoryList;
        private IList<int> memoryStartIndexList;
        private int elementSize;

        internal EntitySystemArray(int elementSize)
        {
            // TODO: Use Array pools array?
            this.elementSize = elementSize;
            this.memoryList = new List<Memory<byte>>();
            this.memoryStartIndexList = new List<int>();
            this.memoryStartIndexList.Add(0);
            this.Length = 0;
        }

        internal EntitySystemArray(EntitySystemArray<byte> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            this.elementSize = source.elementSize;
            this.memoryList = source.memoryList;
            this.memoryStartIndexList = source.memoryStartIndexList;
            this.Length = source.Length;
        }

        public int Length
        {
            get;
            private set;
        }

        public ref T this[int index]
        {
            get
            {
                for (var i = 0; i < this.memoryList.Count; i++)
                {
                    var currentStartIndex = this.memoryStartIndexList[i];
                    var nextStartIndex = this.memoryStartIndexList[i + 1];

                    if (index >= currentStartIndex && index < nextStartIndex)
                    {
                        var offset = (index - currentStartIndex) * elementSize;
                        var dataSpan = this.memoryList[i].Span.Slice(offset, this.elementSize);

                        return ref MemoryMarshal.Cast<byte, T>(dataSpan)[0];
                    }
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        internal void AddMemorySlot(Memory<byte> memorySlot, int entityCount)
        {
            this.Length += entityCount;

            var startIndex = this.memoryStartIndexList[this.memoryList.Count];
            this.memoryStartIndexList.Add(startIndex + entityCount);

            this.memoryList.Add(memorySlot);
        }
    }
}