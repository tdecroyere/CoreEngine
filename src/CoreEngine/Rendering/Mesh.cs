using System;
using System.Collections.Generic;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    public class Mesh : Resource
    {
        public Mesh() : base(0, string.Empty)
        {
        }

        internal Mesh(uint resourceId, string path) : base(resourceId, path)
        {

        }

        public IList<GeometryInstance> GeometryInstances { get; } = new List<GeometryInstance>();
    }
}