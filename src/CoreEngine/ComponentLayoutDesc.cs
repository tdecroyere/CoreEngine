using System;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    internal class ComponentLayoutDesc
    {
        public ComponentLayoutDesc(ComponentLayout componentLayout, int hashCode, Type[] componentTypes)
        {
            this.ComponentLayout = componentLayout;
            this.HashCode = hashCode;
            this.Size = 0;
            this.ComponentCount = componentTypes.Length;
            this.ComponentTypes = new int[this.ComponentCount];
            this.ComponentOffsets = new int[this.ComponentCount];
            this.ComponentSizes = new int[this.ComponentCount];
            this.ComponentDefaultValues = new IComponentData[this.ComponentCount];

            for (int i = 0; i < this.ComponentCount; i++)
            {
                this.ComponentTypes[i] = componentTypes[i].GetHashCode();
                this.ComponentOffsets[i] = this.Size;
                this.ComponentSizes[i] = Marshal.SizeOf(componentTypes[i]);

                this.Size += this.ComponentSizes[i];

                var component = (IComponentData)Activator.CreateInstance(componentTypes[i]);
                component.SetDefaultValues();
                this.ComponentDefaultValues[i] = component;
            }
        }

        public ComponentLayout ComponentLayout { get; }
        public int HashCode { get;}
        public int Size { get; set; }
        public int ComponentCount { get; }
        public int[] ComponentTypes { get; }
        public int[] ComponentOffsets { get; }
        public int[] ComponentSizes { get; }
        public IComponentData[] ComponentDefaultValues { get; }

        public int FindComponentOffset(int componentTypeHash)
        {
            var componentIndex = -1;

            for (var i = 0; i < this.ComponentCount; i++)
            {
                if (this.ComponentTypes[i] == componentTypeHash)
                {
                    componentIndex = i;
                    break;
                }
            }

            if (componentIndex == -1)
            {
                // TODO: Throw error
            }

            return this.ComponentOffsets[componentIndex];
        }

        public int FindComponentSize(int componentTypeHash)
        {
            var componentIndex = -1;

            for (var i = 0; i < this.ComponentCount; i++)
            {
                if (this.ComponentTypes[i] == componentTypeHash)
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
    }
}