using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public class ComponentLayout : IEquatable<ComponentLayout>
    {
        internal ComponentLayout()
        {
            this.LayoutHash = new ComponentHash();
            this.SizeInBytes = 0;
            this.Components = new List<ComponentLayoutItem>();
        }

        public ComponentHash LayoutHash { get; private set; }
        public int SizeInBytes { get; set; }
        public IList<ComponentLayoutItem> Components { get; }

        // TODO: For the moment is it impossible to reorganize the layout after an entity has been created from it
        public bool IsReadOnly { get; internal set; }

        internal void RegisterComponent(ComponentHash componentHash, int componentSize, ReadOnlyMemory<byte>? initialData)
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException("The component layout cannot be changed after an entity has been create from it.");
            }

            for (var i = 0; i < this.Components.Count; i++)
            {
                if (this.Components[i].Hash == componentHash)
                {
                    throw new ArgumentException("The component has already been added to the component layout.");
                }
            }

            ComputeComponentLayoutHashCodeAndSort(componentHash, out var index);

            this.Components.Insert(index, new ComponentLayoutItem(componentHash, this.SizeInBytes, componentSize, initialData));
            this.SizeInBytes += componentSize;

            // TODO: Performance issue here
            var tmp = new ComponentHash[Components.Count];

            for (var i = 0; i < this.Components.Count; i++)
            {
                tmp[i] = this.Components[i].Hash;
            }

            this.LayoutHash = new ComponentHash(tmp);
        }

        public int? FindComponentOffset(ComponentHash componentHash)
        {
            var componentIndex = -1;

            for (var i = 0; i < this.Components.Count; i++)
            {
                if (this.Components[i].Hash == componentHash)
                {
                    componentIndex = i;
                    break;
                }
            }

            if (componentIndex == -1)
            {
                return null;
            }

            return this.Components[componentIndex].Offset;
        }

        public int FindComponentSizeInBytes(ComponentHash componentHash)
        {
            var componentIndex = -1;

            for (var i = 0; i < this.Components.Count; i++)
            {
                if (this.Components[i].Hash == componentHash)
                {
                    componentIndex = i;
                    break;
                }
            }

            if (componentIndex == -1)
            {
                // TODO: Throw error
            }

            return this.Components[componentIndex].SizeInBytes;
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
            return this.LayoutHash.GetHashCode();
        }

        public static bool operator ==(ComponentLayout layout1, ComponentLayout layout2) 
        {
            if (layout1 is not null && layout2 is not null)
            {
                return layout1.LayoutHash == layout2.LayoutHash;
            }

            return false;
        }

        public static bool operator !=(ComponentLayout layout1, ComponentLayout layout2) 
        {
            return !(layout1 == layout2);
        }

        public override string ToString()
        {
            return this.LayoutHash.ToString();
        }

        private void ComputeComponentLayoutHashCodeAndSort(ComponentHash componentHash, out int index)
        {
            index = this.Components.Count;

            for (var i = 0; i < this.Components.Count; i++)
            {
                if (componentHash > this.Components[i].Hash)
                {
                    index = i;
                }
            }
        }
    }
}