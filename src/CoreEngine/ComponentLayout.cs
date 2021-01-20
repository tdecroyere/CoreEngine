using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public class ComponentLayout : IEquatable<ComponentLayout>
    {
        public ComponentLayout(uint id)
        {
            this.EntityComponentLayoutId = id;
            this.LayoutHash = new ComponentHash();
            this.Size = 0;
            this.ComponentCount = 0;
            this.ComponentHashs = new List<ComponentHash>();
            this.ComponentOffsets = new List<int>();
            this.ComponentSizes = new List<int>();
            this.ComponentDefaultValues = new List<ReadOnlyMemory<byte>?>();
        }

        public uint EntityComponentLayoutId { get; }
        public ComponentHash LayoutHash { get; private set; }
        public int Size { get; set; }
        public int ComponentCount { get; private set; }
        public IList<ComponentHash> ComponentHashs { get; }
        public IList<int> ComponentOffsets { get; }
        public IList<int> ComponentSizes { get; }
        internal IList<ReadOnlyMemory<byte>?> ComponentDefaultValues { get; }

        // TODO: For the moment is it impossible to reorganize the layout after an entity has been created from it
        public bool IsReadOnly { get; internal set; }

        internal void RegisterComponent(ComponentHash componentHash, int componentSize, ReadOnlyMemory<byte>? initialData)
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException("The component layout cannot be changed after an entity has been create from it.");
            }

            if (this.ComponentHashs.Contains(componentHash))
            {
                throw new ArgumentException("The component has already been added to the component layout.");
            }

            ComputeComponentLayoutHashCodeAndSort(componentHash, out var index);

            this.ComponentOffsets.Insert(index, this.Size);
            this.ComponentHashs.Insert(index, componentHash);
            this.ComponentSizes.Insert(index, componentSize);

            this.Size += componentSize;

            this.ComponentDefaultValues.Insert(index, initialData);

            // TODO: Performance issue here
            var tmp = new ComponentHash[ComponentHashs.Count];
            this.ComponentHashs.CopyTo(tmp, 0);

            this.LayoutHash = new ComponentHash(tmp);
            this.ComponentCount++;
        }

        public int? FindComponentOffset(ComponentHash componentTypeHash)
        {
            var componentIndex = -1;

            for (var i = 0; i < this.ComponentCount; i++)
            {
                if (this.ComponentHashs[i] == componentTypeHash)
                {
                    componentIndex = i;
                    break;
                }
            }

            if (componentIndex == -1)
            {
                return null;
            }

            return this.ComponentOffsets[componentIndex];
        }

        public int FindComponentSize(ComponentHash componentTypeHash)
        {
            var componentIndex = -1;

            for (var i = 0; i < this.ComponentCount; i++)
            {
                if (this.ComponentHashs[i] == componentTypeHash)
                {
                    componentIndex = i;
                    break;
                }
            }

            if (componentIndex == -1)
            {
                // TODO: Throw error
            }

            return this.ComponentSizes[componentIndex];
        }

        public override bool Equals(object? obj) 
        {
            return obj is ComponentLayout componentLayout && this == componentLayout;
        }

        public bool Equals(ComponentLayout? other)
        {
            return this == other;
        }

        public override int GetHashCode() 
        {
            return this.EntityComponentLayoutId.GetHashCode();
        }

        public static bool operator ==(ComponentLayout layout1, ComponentLayout layout2) 
        {
            return layout1.EntityComponentLayoutId == layout2.EntityComponentLayoutId;
        }

        public static bool operator !=(ComponentLayout layout1, ComponentLayout layout2) 
        {
            return !(layout1 == layout2);
        }

        public override string ToString()
        {
            return this.EntityComponentLayoutId.ToString(NumberFormatInfo.InvariantInfo);
        }

        private void ComputeComponentLayoutHashCodeAndSort(ComponentHash componentHash, out int index)
        {
            index = this.ComponentHashs.Count;

            for (var i = 0; i < this.ComponentHashs.Count; i++)
            {
                if (componentHash > this.ComponentHashs[i])
                {
                    index = i;
                }
            }
        }
    }
}