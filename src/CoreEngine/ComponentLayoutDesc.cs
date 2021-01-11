using System;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    internal class ComponentLayoutDesc
    {
        public ComponentLayoutDesc(ComponentLayout componentLayout, EntityHash hashCode, Type[] componentTypes)
        {
            this.ComponentLayout = componentLayout;
            this.HashCode = hashCode;
            this.Size = 0;
            this.ComponentCount = componentTypes.Length;
            this.ComponentTypes = new EntityHash[this.ComponentCount];
            this.ComponentOffsets = new int[this.ComponentCount];
            this.ComponentSizes = new int[this.ComponentCount];
            this.ComponentDefaultValues = new IComponentData[this.ComponentCount];

            for (int i = 0; i < this.ComponentCount; i++)
            {
                this.ComponentTypes[i] = new EntityHash(componentTypes[i]);
                this.ComponentOffsets[i] = this.Size;
                this.ComponentSizes[i] = Marshal.SizeOf(componentTypes[i]);

                this.Size += this.ComponentSizes[i];

                var component = (IComponentData?)Activator.CreateInstance(componentTypes[i]);

                if (component == null)
                {
                    throw new InvalidOperationException("Cannot create component type.");
                }
                
                component.SetDefaultValues();
                this.ComponentDefaultValues[i] = component;
            }
        }

        public ComponentLayout ComponentLayout { get; }
        public EntityHash HashCode { get;}
        public int Size { get; set; }
        public int ComponentCount { get; }
        public EntityHash[] ComponentTypes { get; }
        public int[] ComponentOffsets { get; }
        public int[] ComponentSizes { get; }
        public IComponentData[] ComponentDefaultValues { get; }

        public int? FindComponentOffset(EntityHash componentTypeHash)
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
                return null;
            }

            return this.ComponentOffsets[componentIndex];
        }

        public int FindComponentSize(EntityHash componentTypeHash)
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