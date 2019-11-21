using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoreEngine.Collections;

namespace CoreEngine
{
    internal class EntitySystemData
    {
        // TODO: Use array pool to reuse memory?
        public EntitySystemArray<Entity> EntityArray { get; } = new EntitySystemArray<Entity>(Marshal.SizeOf<Entity>());
        public IDictionary<int, EntitySystemArray<byte>> ComponentsData { get; } = new Dictionary<int, EntitySystemArray<byte>>();
    }
}