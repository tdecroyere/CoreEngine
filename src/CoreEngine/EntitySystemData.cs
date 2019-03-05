using System;
using System.Collections.Generic;

namespace CoreEngine
{
    internal class EntitySystemData
    {
        // TODO: Storing ArrayPool buffers here is a not great, we do that for the moment so the API is cleaner
        public Entity[] entitiesArray = new Entity[0];
        public IDictionary<int, byte[]> componentsData = new Dictionary<int, byte[]>();
    }
}