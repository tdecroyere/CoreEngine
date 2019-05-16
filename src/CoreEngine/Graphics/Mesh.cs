using System;
using System.Collections.Generic;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Mesh : Resource
    {
        public Mesh(string path) : base(path)
        {
        }

        public IList<MeshSubObject> SubObjects { get; } = new List<MeshSubObject>();
    }
}