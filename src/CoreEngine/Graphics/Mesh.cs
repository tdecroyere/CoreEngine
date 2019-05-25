using System;
using System.Collections.Generic;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Mesh : Resource
    {
        internal Mesh(uint resourceId, string path) : base(resourceId, path)
        {
        }

        public IList<MeshSubObject> SubObjects { get; } = new List<MeshSubObject>();
    }
}